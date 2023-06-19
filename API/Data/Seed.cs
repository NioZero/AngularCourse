using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Data;

public static class Seed
{
    public static async Task ClearConnections(DataContext context)
    {
        context.Connections.RemoveRange(context.Connections);

        await context.SaveChangesAsync();
    }

    public static async Task SeedUsers(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        if(await userManager.Users.AnyAsync()) return;

         var userData = await File.ReadAllTextAsync("Data/UserSeedData.json");

         var users = JsonConvert.DeserializeObject<List<AppUser>>(userData);

        var roles = new List<AppRole>()
        {
            new AppRole { Name = "Member" },
            new AppRole { Name = "Admin" },
            new AppRole { Name = "Moderator" }
        };

        foreach(var role in roles)
        {
            await roleManager.CreateAsync(role);
        }

        foreach(var user in users)
        {
            user.UserName = user.UserName.ToLower();
            user.DateOfBirth = DateTime.SpecifyKind(user.DateOfBirth, DateTimeKind.Utc);
            user.Created = DateTime.SpecifyKind(user.Created, DateTimeKind.Utc);
            user.LastActive = DateTime.SpecifyKind(user.LastActive, DateTimeKind.Utc);

            await userManager.CreateAsync(user, "123456");
            await userManager.AddToRoleAsync(user, "Member");
        }

        /*foreach(var user in userManager.Users.ToList())
        {
            Console.WriteLine($"Reset password for user '{user.UserName}'");

            var result = await userManager.RemovePasswordAsync(user);

            Console.WriteLine($"Reset password result: {result.Succeeded}");
            foreach(var error in result.Errors)
                Console.WriteLine($"{error.Code}: {error.Description}");

            Console.WriteLine();
        }*/

        /*foreach(var user in userManager.Users.ToList())
        {
            //user.UserName = user.UserName.ToLower();

            Console.WriteLine($"Set default password for user '{user.UserName}'");

            await userManager.RemovePasswordAsync(user);

            var result = await userManager.AddPasswordAsync(user, "123456");

            Console.WriteLine($"Set password result: {result.Succeeded}");
            foreach(var error in result.Errors)
                Console.WriteLine($"{error.Code}: {error.Description}");

            Console.WriteLine();
        }*/

        foreach(var user in userManager.Users.ToList())
        {
            await userManager.AddToRoleAsync(user, "Member");
        }

        var admin = userManager.Users.SingleOrDefault(u => u.UserName == "ghidalgo");
        if(admin != null)
        {
            await userManager.AddToRolesAsync(admin, new[] { "Admin", "Moderator" });
        }
        else
        {
            admin = new AppUser()
            {
                UserName = "ghidalgo",
                KnownAs = "NioZero",
                City = "Concepción",
                Country = "Chile"
            };

            admin.Created = DateTime.SpecifyKind(admin.Created, DateTimeKind.Utc);
            admin.LastActive = DateTime.SpecifyKind(admin.LastActive, DateTimeKind.Utc);

            await userManager.CreateAsync(admin, "123456");
            await userManager.AddToRoleAsync(admin, "Member");
        }
    }
}