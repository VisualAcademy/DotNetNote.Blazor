﻿using DotNetNote.Areas.Identity;
using DotNetNote.Data;
using DotNetNote.Models;
using DotNetNote.Services;
using DotNetNote.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetNote
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
            //services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();
            //services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
            services.AddIdentity<ApplicationUser, ApplicationRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
            services.AddControllersWithViews(); // MVC 3.1
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>>();
            services.AddSingleton<WeatherForecastService>();

            //[!] Configuration: JSON 파일의 데이터를 POCO 클래스에 주입
            services.Configure<DotNetNoteSettings>(Configuration.GetSection("DotNetNoteSettings"));

            // ============================================================================== // 
            // 새로운 DbContext 추가
            services.AddEntityFrameworkSqlServer().AddDbContext<DotNetNoteContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
            // ============================================================================== // 

            services.AddTransient<IEmailSender, EmailSender>();

            //[DI] 의존성 주입(Dependency Injection)
            DependencyInjectionContainer(services);
        }

        /// <summary>
        /// 의존성 주입 관련 코드만 따로 모아서 관리
        /// - 리포지토리 등록
        /// </summary>
        private void DependencyInjectionContainer(IServiceCollection services)
        {
            //[?] ConfigureServices가 호출되기 전에는 DI(종속성 주입)가 설정되지 않습니다.

            //[DNN][!] Configuration 개체 주입: 
            //    IConfiguration 또는 IConfigurationRoot에 Configuration 개체 전달
            //    appsettings.json 파일의 데이터베이스 연결 문자열을 
            //    리포지토리 클래스에서 사용할 수 있도록 설정
            // IConfiguration 주입 -> Configuration의 인스턴스를 실행 
            services.AddSingleton<IConfiguration>(Configuration);

            //[DNN][1] 게시판 관련 서비스 등록            
            //[1] 생성자에 문자열로 연결 문자열 지정
            //services.AddSingleton<INoteRepository>(
            //  new NoteRepository(
            //      Configuration["Data:DefaultConnection:ConnectionString"]));            
            //[2] 의존성 주입으로 Configuration 주입
            //[a] NoteRepository에서 IConfiguration으로 데이터베이스 연결 문자열 접근
            services.AddTransient<INoteRepository, NoteRepository>();
            //[b] CommentRepository의 생성자에 데이터베이스 연결문자열 직접 전송
            //services.AddSingleton<INoteCommentRepository>(
            //    new NoteCommentRepository(
            //        Configuration["ConnectionStrings:DefaultConnection"]));
            //[b] 위 코드를 아래 코드로 변경
            services.AddTransient<INoteCommentRepository, NoteCommentRepository>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                //endpoints.MapControllerRoute(
                //    name: "default",
                //    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            CreateBuiltInUsersAndRoles(serviceProvider).Wait();
        }

        private async Task CreateBuiltInUsersAndRoles(IServiceProvider serviceProvider)
        {
            //[0] DbContext 개체 생성
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.EnsureCreated(); // 데이터베이스가 생성되어 있는지 확인 

            // 기본 내장 사용자 및 역할이 하나도 없으면(즉, 처음 데이터베이스 생성이라면)
            if (!dbContext.Users.Any() && !dbContext.Roles.Any())
            {
                string domainName = "a.com";

                //[1] Groups(Roles)
                var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                //[1][1] ('Administrators', '관리자 그룹', 'Group', '응용 프로그램을 총 관리하는 관리 그룹 계정')
                //[1][2] ('Everyone', '전체 사용자 그룹', 'Group', '응용 프로그램을 사용하는 모든 사용자 그룹 계정')
                //[1][3] ('Users', '일반 사용자 그룹', 'Group', '일반 사용자 그룹 계정')
                //[1][4] ('Guests', '관리자 그룹', 'Group', '게스트 사용자 그룹 계정')
                string[] roleNames = { "Administrators", "Everyone", "Users", "Guests", "Managers" };
                foreach (var roleName in roleNames)
                {
                    var roleExist = await roleManager.RoleExistsAsync(roleName);
                    if (!roleExist)
                    {
                        await roleManager.CreateAsync(new ApplicationRole { Name = roleName, NormalizedName = roleName.ToUpper(), Description = "" }); // 빌트인 그룹 생성
                    }
                }

                //[2] Users
                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                //[2][1] Administrator
                // ('Administrator', '관리자', 'User', '응용 프로그램을 총 관리하는 사용자 계정')
                ApplicationUser administrator = await userManager.FindByEmailAsync($"administrator@{domainName}");
                if (administrator == null)
                {
                    administrator = new ApplicationUser()
                    {
                        UserName = $"administrator@{domainName}",
                        Email = $"administrator@{domainName}",
                        EmailConfirmed = true,
                    };
                    await userManager.CreateAsync(administrator, "Pa$$w0rd");
                }

                //[2][2] Guest
                // ('Guest', '게스트 사용자', 'User', '게스트 사용자 계정')
                ApplicationUser guest = await userManager.FindByEmailAsync($"guest@{domainName}");
                if (guest == null)
                {
                    guest = new ApplicationUser()
                    {
                        UserName = "Guest",
                        Email = $"guest@{domainName}",
                    };
                    await userManager.CreateAsync(guest, "Pa$$w0rd");
                }

                //[2][3] Anonymous
                // ('Anonymous', '익명 사용자', 'User', '익명 사용자 계정')
                ApplicationUser anonymous = await userManager.FindByEmailAsync($"anonymous@{domainName}");
                if (anonymous == null)
                {
                    anonymous = new ApplicationUser()
                    {
                        UserName = "Anonymous",
                        Email = $"anonymous@{domainName}",
                    };
                    await userManager.CreateAsync(anonymous, "Pa$$w0rd");
                }

                //[2][4] User
                // ('User', '일반사용자', 'User', '응용 프로그램에 로그인할 수 있는 사용자')
                ApplicationUser user = await userManager.FindByEmailAsync($"user@{domainName}");
                if (user == null)
                {
                    user = new ApplicationUser()
                    {
                        UserName = $"user@{domainName}",
                        Email = $"user@{domainName}",
                        EmailConfirmed = true,
                    };
                    await userManager.CreateAsync(user, "Pa$$w0rd");
                }

                //[2][5] Manager
                // ('User', '일반사용자', 'User', '응용 프로그램에 로그인할 수 있는 사용자')
                ApplicationUser manager = await userManager.FindByEmailAsync($"manager@{domainName}");
                if (manager == null)
                {
                    manager = new ApplicationUser()
                    {
                        UserName = $"manager@{domainName}",
                        Email = $"manager@{domainName}",
                        EmailConfirmed = true,
                    };
                    await userManager.CreateAsync(manager, "Pa$$w0rd");
                }

                //[3] UsersInRoles: AspNetUserRoles Table
                await userManager.AddToRoleAsync(administrator, "Administrators");
                await userManager.AddToRoleAsync(administrator, "Users");
                await userManager.AddToRoleAsync(guest, "Guests");
                await userManager.AddToRoleAsync(anonymous, "Guests");
                await userManager.AddToRoleAsync(user, "Users");
                await userManager.AddToRoleAsync(manager, "Managers");
            }
        }
    }
}
