using API.Entities;
using Microsoft.AspNetCore.Identity;

namespace API.Data;

public static class Seed
{
    public static async Task SeedUsers(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        //if(await context.Users.AnyAsync()) return;

        // var userData = await File.ReadAllTextAsync("Data/UserSeedData.json");

        // var users = JsonConvert.DeserializeObject<List<AppUser>>(userData);


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
    }
}