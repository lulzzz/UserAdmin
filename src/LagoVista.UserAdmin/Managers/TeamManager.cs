﻿using LagoVista.Core.Managers;
using LagoVista.UserAdmin.Interfaces.Managers;
using System;
using System.Collections.Generic;
using System.Text;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.UserAdmin.Models.Orgs;
using System.Threading.Tasks;
using LagoVista.UserAdmin.Interfaces.Repos.Orgs;
using LagoVista.Core.Interfaces;
using LagoVista.Core.PlatformSupport;
using LagoVista.UserAdmin.Interfaces.Repos.Account;
using static LagoVista.Core.Models.AuthorizeResult;

namespace LagoVista.UserAdmin.Managers
{
    public class TeamManager : ManagerBase, ITeamManager
    {
        IAppUserRepo _appUserRepo;
        ITeamRepo _teamRepo;
        ITeamAccountRepo _teamAccountRepo;

        public TeamManager(IAppUserRepo appUserRepo, ITeamRepo teamRepo, ITeamAccountRepo teamAccountRepo, IDependencyManager depManager, ISecurity security, ILogger logger, IAppConfig appConfig) : base(logger, appConfig, depManager, security)
        {
            _appUserRepo = appUserRepo;
            _teamRepo = teamRepo;
            _teamAccountRepo = teamAccountRepo;
        }

        public async Task<InvokeResult> AddTeamAsync(Team team, EntityHeader org, EntityHeader user)
        {
            await AuthorizeAsync(team, AuthorizeActions.Create, user, org);
            ValidationCheck(team, Actions.Create);
            await _teamRepo.AddTeamAsync(team);
            return InvokeResult.Success;
        }

        public async Task<InvokeResult> UpdateTeamAsync(Team team, EntityHeader org, EntityHeader user)
        {
            await AuthorizeAsync(team, AuthorizeActions.Update, user, org);
            ValidationCheck(team, Actions.Update);
            await _teamRepo.UpdateTeamAsync(team);
            return InvokeResult.Success;
        }

        public async Task<InvokeResult> DeleteTeamAsync(String teamId, EntityHeader org, EntityHeader user)
        {
            var assetSet = await _teamRepo.GetTeamAsync(teamId);
            await AuthorizeAsync(assetSet, AuthorizeActions.Delete, user, org);
            await ConfirmNoDepenenciesAsync(assetSet);
            await _teamRepo.DeleteTeamAsync(teamId);
            return InvokeResult.Success;
        }

        public async Task<Team> GetTeamAsync(string id, EntityHeader org, EntityHeader user)
        {
            var team = await _teamRepo.GetTeamAsync(id);
            await AuthorizeAsync(team, AuthorizeActions.Read, user, org);
            return team;
        }

        public async Task<IEnumerable<TeamSummary>> GetTeamsForOrgAsync(string orgId, EntityHeader org, EntityHeader user)
        {
            await AuthorizeOrgAccess(user, orgId, typeof(Team));
            return await _teamRepo.GetTeamsForOrgAsync(orgId);
        }

        public  Task<bool> QueryKeyInUseAsync(string key, EntityHeader org)
        {
            return _teamRepo.QueryKeyInUseAsync(key, org.Id);
        }

        public async Task<DependentObjectCheckResult> CheckTeamInUseAsync(string teamId, EntityHeader org, EntityHeader user)
        {
            var team = await _teamRepo.GetTeamAsync(teamId);
            await AuthorizeAsync(team, AuthorizeResult.AuthorizeActions.Read, user, org);
            return await CheckForDepenenciesAsync(team);
        }


        public async  Task<IEnumerable<TeamAccountSummary>> GetMembersForTeamAsync(string teamId, EntityHeader org, EntityHeader user)
        {
            await AuthorizeOrgAccess(user, org.Id, typeof(Team));
            return await _teamAccountRepo.GetTeamMembersAsync(teamId);
        }    

        public async Task<InvokeResult> AddTeamMemberAsync(string teamId, string accountId, EntityHeader org, EntityHeader addedByMemberId)
        {
            var team = await _teamRepo.GetTeamAsync(teamId);
            await AuthorizeAsync(team, AuthorizeResult.AuthorizeActions.Update, addedByMemberId, org);
            var member = await _appUserRepo.FindByIdAsync(accountId);
            var teamMember = new TeamAccount(team.ToEntityHeader(), member.ToEntityHeader());
            await _teamAccountRepo.AddTeamMemberAsync(teamMember);
            return InvokeResult.Success;
        }        

        public async Task<InvokeResult> RemoveTeamMemberAsync(string teamId, string accountId, EntityHeader org, EntityHeader addedByMemberId)
        {
            var team = await _teamRepo.GetTeamAsync(teamId);
            await AuthorizeAsync(team, AuthorizeResult.AuthorizeActions.Update, addedByMemberId, org);

            await _teamAccountRepo.RemoveMemberAsync(teamId, accountId);
            return InvokeResult.Success;
        }
    }
}