using System.Security.Claims;
using ClickSphere_API.Models;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using ClickSphere_API.Tools;

namespace ClickSphere_API.Services
{
    /**
     * This class is used to manage user accounts
     */
    public class ApiUserService(IDbService dbService) : IApiUserService
    {
        private readonly IDbService _dbService = dbService;

        /**
        * This method creates a new user
        * @param username The username of the user
        * @param password The password of the user
        * @return True if the user was created, otherwise false
        */
        public async Task<Result> CreateUser(string username, string password)
        {
            // check if user exists
            string query = $"SELECT name FROM system.users WHERE name = '{username}'";
            var result = await _dbService.ExecuteScalar(query);
            if (result is not DBNull)
                return Result.BadRequest("User already exists");

            // create the user
            query = $"CREATE USER {username} IDENTIFIED BY '{password}'";
            await _dbService.ExecuteNonQuery(query);

            // get the new user's id
            query = $"SELECT id FROM system.users WHERE name = '{username}'";
            result = await _dbService.ExecuteScalar(query);
            if (result is DBNull)
                return Result.BadRequest("Could not create user");
            
            var userId = result!.ToString();

            // create the user's configuration
            query = $"INSERT INTO ClickSphere.Users (Id, UserName, LDAP_User, Email, FirstName, LastName, Phone, Department) VALUES ('{userId}', '{username}', '', '', '', '', '', '')";
            await _dbService.ExecuteNonQuery(query);

            // check if insert was successful
            query = $"SELECT UserName FROM ClickSphere.Users WHERE UserName = '{username}'";
            result = await _dbService.ExecuteScalar(query);
            if (result is DBNull)
                return Result.BadRequest("Could not create user");

            // assign the default role to the user
            query = $"GRANT ROLE default TO USER {username}";
            try
            {
                await _dbService.ExecuteNonQuery(query);
            }
            catch(Exception)
            {
                return Result.BadRequest("Could not assign default role");
            }
            return Result.Ok();
        }

        /**
        * This method is used to check if the login credentials are valid
        * @param username The username of the user
        * @param password The password of the user
        * @return A ClaimsPrincipal object that contains the user's claims if the login credentials are valid, otherwise null
        */
        public async Task<ClaimsPrincipal?> CheckLogin(string username, string password)
        {
            if (await _dbService.CheckLogin(username, password))
            {
                var claimsPrincipal = new ClaimsPrincipal(
                  new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, username)],
                    BearerTokenDefaults.AuthenticationScheme
                  ));
                return claimsPrincipal;
            }
            return null;
        }

        /**
        * This method retrieves all users from the Users table
        * @return A list of User objects representing the users in the table
        */
        public async Task<List<UserConfig>> GetUsers()
        {
            string query = "SELECT toString(Id) as Id, UserName, LDAP_User, Email, FirstName, LastName, Phone, Department from ClickSphere.Users";
            var result = await _dbService.ExecuteQueryDictionary(query);

            List<UserConfig> users = [];
            foreach (var row in result)
            {
                UserConfig user = new()
                {
                    Id = Guid.Parse(row["Id"].ToString()!),
                    Username = row["UserName"].ToString()!,
                    LDAP_User = row["LDAP_User"]?.ToString()!,
                    Email = row["Email"]?.ToString()!,
                    FirstName = row["FirstName"]?.ToString()!,
                    LastName = row["LastName"]?.ToString()!,
                    Phone = row["Phone"]?.ToString()!,
                    Department = row["Department"]?.ToString()!
                };

                users.Add(user);
            }
            return users;
        }

        /**
        * This method retrieves all roles from the UserRoles table
        * @return A list of strings representing all roles
        */
        public async Task<List<Role>> GetRoles()
        {
            string query = $"SELECT id, name FROM system.roles";
            var result = await _dbService.ExecuteQueryDictionary(query);

            List<Role> roles = result.Select(row => new Role
            {
                RoleId = Guid.Parse(row["id"].ToString()!),
                RoleName = row["name"].ToString()!
            }).ToList();

            return roles;
        }

        /**
        * This method retrieves the roles associated with a user from the UserRoles table
        * @param userName The user name of the user
        * @return A list of strings representing the roles associated with the user
        */
        public async Task<Role?> GetUserRole(string userName)
        {
            string query = $"SELECT granted_role_name, granted_role_id from system.role_grants where user_name = '{userName}'";
            var result = await _dbService.ExecuteQueryDictionary(query);
            var role = result.Select(row => new Role
            {
                RoleId = Guid.Parse(row["granted_role_id"].ToString()!),
                RoleName = row["granted_role_name"].ToString()!
            }).FirstOrDefault();

            return role;
        }

        /**
        * This method assigns a role to a user
        * @param userName The user name of the user
        * @param roleName The name of the role to assign
        * @return True if the role was assigned, otherwise false
        */
        public async Task<Result> AssignRole(string userName, string roleName)
        {
            string query = $"GRANT ROLE {roleName} TO USER {userName}";
            try 
            {
                await _dbService.ExecuteNonQuery(query);
            }
            catch(Exception)
            {
                return Result.BadRequest("Could not assign role");
            }
            return Result.Ok();
        }

        /**
        * This method removes a role from a user
        * @param userName The user name of the user
        * @param roleName The name of the role to remove
        * @return True if the role was removed, otherwise false
        */
        public async Task<Result> RemoveRole(string userName, string roleName)
        {
            string query = $"REVOKE ROLE {roleName} FROM USER {userName}";
            try 
            {
                await _dbService.ExecuteNonQuery(query);
            }
            catch(Exception)
            {
                return Result.BadRequest("Could not remove role");
            }
            return Result.Ok();
        }

        /**
        * This method deletes a user
        * @param userName The user name of the user
        * @return True if the user was deleted, otherwise false
        */
        public async Task<Result> DeleteUser(string username)
        {
            string query = $"DROP USER {username}";
            try 
            {
                await _dbService.ExecuteNonQuery(query);
            }
            catch(Exception)
            {
            }

            query = $"DELETE FROM ClickSphere.Users WHERE UserName = '{username}'";

            try 
            {
                await _dbService.ExecuteNonQuery(query);
            }
            catch(Exception)
            {
            }

            // check if user was deleted
            query = $"SELECT name FROM system.users WHERE name = '{username}'";
            var result = await _dbService.ExecuteScalar(query);
            if (result is not DBNull)
                return Result.BadRequest("Could not delete user");
            else
                return Result.Ok();
        }

        /**
        * This method updates the password of a user
        * @param userName The user name of the user
        * @param newPassword The new password of the user
        * @return True if the password was updated, otherwise false
        */
        public async Task<Result> UpdatePassword(string userName, string newPassword)
        {
            string query = $"ALTER USER {userName} IDENTIFIED BY '{newPassword}'";
            var result = await _dbService.ExecuteNonQuery(query);
            return result > 0 ? Result.Ok() : Result.BadRequest("Could not update password");
        }

        /**
        * This method gets the user configuration from ClickSphere.Users
        * @param userName The user name of the user
        * @return The user configuration
        */
        public async Task<UserConfig?> GetUserConfig(Guid userId)
        {
            string query = $"SELECT * FROM ClickSphere.Users WHERE Id = '{userId}'";
            var result = await _dbService.ExecuteQueryDictionary(query);
            var userConfig = result.Select(row => new UserConfig
            {
                Id = Guid.Parse(row["Id"].ToString()!),
                Username = row["UserName"].ToString()!,
                LDAP_User = row["LDAP_User"].ToString()!,
                FirstName = row["FirstName"].ToString()!,
                LastName = row["LastName"].ToString()!,
                Email = row["Email"].ToString()!,
                Phone = row["Phone"].ToString()!,
                Department = row["Department"].ToString()!
            }).FirstOrDefault();

            return userConfig;
        }

        /**
        * This method updates the user configuration in ClickSphere.Users
        * @param user The user configuration to update
        * @return True if the user configuration was updated, otherwise false
        */
        public async Task<Result> UpdateUser(UserConfig user)
        {
            string query = $"ALTER TABLE ClickSphere.Users UPDATE " +
                           $"UserName = '{user.Username}'," +
                           $"LDAP_User = '{user.LDAP_User}'," +
                           $"FirstName = '{user.FirstName}'," + 
                           $"LastName = '{user.LastName}'," +
                           $"Email = '{user.Email}'," +
                           $"Phone = '{user.Phone}'," +
                           $"Department = '{user.Department}' " +
                           $"WHERE Id = '{user.Id}'";
            var nReturn = await _dbService.ExecuteNonQuery(query);
            return nReturn == 0 ? Result.Ok() : Result.BadRequest("Could not update user");
        }
    }
}