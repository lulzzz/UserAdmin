﻿using LagoVista.AspNetCore.Identity.Interfaces;
using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.AspNetCore.Identity.Models;
using LagoVista.Core.Authentication.Models;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.UserAdmin.Interfaces.Repos.Security;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using LagoVista.Core;
using System.Text;
using System.Threading.Tasks;
using LagoVista.Core.Validation;
using LagoVista.UserAdmin.Interfaces.Repos.Apps;
using LagoVista.Core.Models;
using LagoVista.UserAdmin.Interfaces.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LagoVista.UserAdmin.Models.Apps;

namespace LagoVista.UserAdmin.Tests.TokenTests
{
    [TestClass]
    public class AuthTokenTests
    {
        Mock<IUserManager> _userManager;
        Mock<ISignInManager> _signInManager;
        Mock<IRefreshTokenManager> _refreshTokenManager;
        Mock<ITokenHelper> _tokenHelper;
        Mock<IOrgHelper> _orgHelper;
        Mock<IAppInstanceManager> _appInstanceManager;
        Mock<IAuthRequestValidators> _authRequestValidators;

        AuthTokenManager _authTokenManager;

        [TestInitialize]
        public void Init()
        {
            _signInManager = new Mock<ISignInManager>();
            _userManager = new Mock<IUserManager>();
            _refreshTokenManager = new Mock<IRefreshTokenManager>();
            _tokenHelper = new Mock<ITokenHelper>();
            _orgHelper = new Mock<IOrgHelper>();
            _appInstanceManager = new Mock<IAppInstanceManager>();
            _authRequestValidators = new Mock<IAuthRequestValidators>();

            var tokenOptions = new TokenAuthOptions()
            {
                AccessExpiration = TimeSpan.FromMinutes(90),
                RefreshExpiration = TimeSpan.FromDays(90),
            };

            _authTokenManager = new AuthTokenManager(new Mock<IAppInstanceRepo>().Object, _refreshTokenManager.Object, _authRequestValidators.Object, _orgHelper.Object, _tokenHelper.Object, _appInstanceManager.Object, new Mock<IAdminLogger>().Object, _signInManager.Object, _userManager.Object);

            _appInstanceManager.Setup(ais => ais.UpdateLastLoginAsync(It.IsAny<string>(), It.IsAny<AuthRequest>())).ReturnsAsync(InvokeResult<AppInstance>.Create(new AppInstance("rowid","userid")));
            _appInstanceManager.Setup(ais => ais.UpdateLastAccessTokenRefreshAsync(It.IsAny<string>(), It.IsAny<AuthRequest>())).ReturnsAsync(InvokeResult<AppInstance>.Create(new AppInstance("rowid", "userid")));

            _signInManager.Setup(sim => sim.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(Task.FromResult(InvokeResult.Success));
            _userManager.Setup(usm => usm.FindByIdAsync(It.IsAny<string>())).Returns(Task.FromResult(new AppUser() { Id = Guid.NewGuid().ToId() }));
            _userManager.Setup(usm => usm.FindByNameAsync(It.IsAny<string>())).Returns(Task.FromResult(new AppUser() { Id = Guid.NewGuid().ToId() }));
            _orgHelper.Setup(ohlp => ohlp.SetUserOrgAsync(It.IsAny<AuthRequest>(), It.IsAny<AppUser>())).Returns(Task.FromResult(InvokeResult.Success));
            _refreshTokenManager.Setup(rtm => rtm.GenerateRefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task<RefreshToken>.FromResult(InvokeResult<RefreshToken>.Create(new RefreshToken("XXXX"))));
            _authRequestValidators.Setup(arv => arv.ValidateAuthRequest(It.IsAny<AuthRequest>())).Returns(InvokeResult.Success);
            _authRequestValidators.Setup(arv => arv.ValidateAccessTokenGrant(It.IsAny<AuthRequest>())).Returns(InvokeResult.Success);
            _authRequestValidators.Setup(arv => arv.ValidateRefreshTokenGrant(It.IsAny<AuthRequest>())).Returns(InvokeResult.Success);
            _tokenHelper.Setup(tlp => tlp.GenerateAuthResponse(It.IsAny<AppUser>(), It.IsAny<AuthRequest>(), It.IsAny<InvokeResult<RefreshToken>>())).Returns(new InvokeResult<AuthResponse>()
            {
                Result = new AuthResponse()
                {
                    AccessToken = "ACC",
                    AccessTokenExpiresUTC = DateTime.Now.AddMinutes(30).ToJSONString(),
                }
            });
        }

        //TODO: SHould write some tests here but behind schedule...did deskcheck of code and after refactoring its very straight forward.


        [TestMethod]
        public async Task ShouldGenerateAccessToken()
        {
            var request = new AuthRequest()
            {
                AppId = "APP123",
                AppInstanceId = "INST12",
                ClientType = "APP",
                DeviceId = "DEV001",
                Email = "email@address.net",
                GrantType = "password",
                OrgId = "org12",
                OrgName = "orgname",
                Password = "pwd",
                UserName = "email@foo.net"

            };

            var result = await _authTokenManager.AccessTokenGrantAsync(request);
            Assert.IsTrue(result.Successful);
        }


        [TestMethod]
        public async Task ShouldGenerateRefreshToken()
        {
            var request = new AuthRequest()
            {
                AppId = "APP123",
                AppInstanceId = "INST12",
                ClientType = "APP",
                DeviceId = "DEV001",
                Email = "email@address.net",
                GrantType = "refreshtoken",
                OrgId = "org12",
                OrgName = "orgname",
                Password = "pwd",
                UserName = "email@foo.net"

            };

            var result = await _authTokenManager.RefreshTokenGrantAsync(request);
            Assert.IsTrue(result.Successful);
        }
    }
}
