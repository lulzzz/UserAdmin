﻿using LagoVista.CloudStorage.DocumentDB;
using LagoVista.Core.PlatformSupport;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.UserAdmin.Interfaces.Managers;
using LagoVista.UserAdmin.Interfaces.Repos.Users;
using LagoVista.UserAdmin.Models.Users;
using System;
using System.Linq;
using System.Threading.Tasks;
using LagoVista.Core.Models;
using System.Collections.Generic;
using Microsoft.Azure.Documents;
using LagoVista.UserAdmin.Models.Orgs;

namespace LagoVista.UserAdmin.Repos.Users
{
    public class AppUserRepo : DocumentDBRepoBase<AppUser>, IAppUserRepo
    {
        bool _shouldConsolidateCollections;
        IRDBMSManager _rdbmsUserManager;
        IUserAdminSettings _adminSettings;

        public AppUserRepo(IRDBMSManager rdbmsUserManager, IUserAdminSettings userAdminSettings, IAdminLogger logger) :
            base(userAdminSettings.UserStorage.Uri, userAdminSettings.UserStorage.AccessKey, userAdminSettings.UserStorage.ResourceName, logger)
        {
            _adminSettings = userAdminSettings;
            _shouldConsolidateCollections = userAdminSettings.ShouldConsolidateCollections;
            _rdbmsUserManager = rdbmsUserManager;
        }

        protected override bool ShouldConsolidateCollections
        {
            get { return _shouldConsolidateCollections; }
        }

        public async Task CreateAsync(AppUser user)
        {
            await CreateDocumentAsync(user);
            await _rdbmsUserManager.AddAppUserAsync(user);
        }

        public async Task DeleteAsync(AppUser user)
        {
            await DeleteAsync(user);
        }

        public Task<AppUser> FindByIdAsync(string id)
        {
            return GetDocumentAsync(id, false);
        }

        public async Task<AppUser> FindByNameAsync(string userName)
        {
            if(String.IsNullOrEmpty(userName))
            {
                throw new InvalidOperationException("Attempt to find user with null or empty user name.");
            }

            var user = (await QueryAsync(usr => usr.UserName == userName.ToUpper())).FirstOrDefault();
            if(user == null)
            {
                return null;
            }

            //TODO: THIS SUX, when deserializing the query it auto converts to date time, we want the json string
            return await FindByIdAsync(user.Id);
        }

        public async Task<AppUser> FindByEmailAsync(string email)
        {
            if (String.IsNullOrEmpty(email))
            {
                throw new InvalidOperationException("Attempt to find user with null or empty user name.");
            }

            var user = (await QueryAsync(usr => usr.Email == email.ToUpper())).FirstOrDefault();
            if(user == null)
            {
                return null;
            }

            //TODO: THIS SUX, when deserializing the query it auto converts to date time, we want the json string
            return await FindByIdAsync(user.Id);
        }

        public async Task UpdateAsync(AppUser user)
        {
            await Client.UpsertDocumentAsync(await GetCollectionDocumentsLinkAsync(), user);
            await _rdbmsUserManager.UpdateAppUserAsync(user);
        }

        public Task<AppUser> FindByThirdPartyLogin(string providerId, string providerKey)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<UserInfoSummary>> GetUserSummaryForListAsync(IEnumerable<OrgUser> orgUsers)
        {
            var sqlParams = string.Empty;
            var idx = 0;
            var paramCollection = new SqlParameterCollection();
            foreach (var orgUser in orgUsers)
            {
                if (!String.IsNullOrEmpty(sqlParams))
                {
                    sqlParams += ",";
                }
                var paramName = $"@userId{idx++}";

                sqlParams += paramName;
                paramCollection.Add(new SqlParameter(paramName, orgUser.UserId));
            }

            sqlParams.TrimEnd(',');

            //TODO: This seems kind of ugly...need to put more thought into this, this shouldn't be a query that is hit very often
            var query = $"SELECT * FROM c where c.id in ({sqlParams})";

            /* this sorta sux, but oh well */
            var appUsers = await QueryAsync(query, paramCollection);
            var userSummaries = from appUser
                                in appUsers
                                join orgUser
                                in orgUsers on appUser.Id equals orgUser.UserId
                                select appUser.ToUserInfoSummary(orgUser.IsOrgAdmin);

            return userSummaries;
        }
    }
}
