using System;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Security;
using MongoDB.Driver;
using Smart_Campus_Service_Portal.Models;

namespace Smart_Campus_Service_Portal.Helpers
{
    public static class AuthSessionHelper
    {
        private const string SessionUserId = "Auth_UserId";
        private const string SessionUserName = "Auth_UserName";
        private const string SessionUserEmail = "Auth_UserEmail";
        private const string SessionUserRole = "Auth_UserRole";
        private const string SessionUserFullName = "Auth_UserFullName";

        public static void SignIn(User user, bool createPersistentCookie = false)
        {
            if (user == null) return;

            var session = HttpContext.Current?.Session;
            if (session != null)
            {
                session[SessionUserId] = user.Id;
                session[SessionUserName] = user.Username;
                session[SessionUserEmail] = user.Email;
                session[SessionUserRole] = user.Role;
                session[SessionUserFullName] = user.FullName;
            }

            // Also set FormsAuthentication ticket for compatibility
            var ticket = new FormsAuthenticationTicket(
                1,
                user.Email,
                DateTime.Now,
                DateTime.Now.AddMinutes(120),
                createPersistentCookie,
                $"{user.Id}|{user.Role}|{user.FullName}"
            );

            string encryptedTicket = FormsAuthentication.Encrypt(ticket);
            var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
            {
                HttpOnly = true,
                Expires = createPersistentCookie ? ticket.Expiration : DateTime.MinValue
            };

            HttpContext.Current?.Response.Cookies.Add(authCookie);
        }

        public static void SignOut()
        {
            var session = HttpContext.Current?.Session;
            if (session != null)
            {
                session.Clear();
                session.Abandon();
            }

            FormsAuthentication.SignOut();

            var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, "")
            {
                Expires = DateTime.Now.AddDays(-1)
            };
            HttpContext.Current?.Response.Cookies.Add(authCookie);
        }

        public static bool IsAuthenticated
        {
            get
            {
                var session = HttpContext.Current?.Session;
                if (session != null && session[SessionUserId] != null)
                    return true;

                return TryRestoreFromCookie();
            }
        }

        public static string CurrentUserId
        {
            get
            {
                var session = HttpContext.Current?.Session;
                if (session?[SessionUserId] != null)
                    return session[SessionUserId].ToString();

                TryRestoreFromCookie();
                return HttpContext.Current?.Session?[SessionUserId]?.ToString();
            }
        }

        public static string CurrentUserName
        {
            get
            {
                var session = HttpContext.Current?.Session;
                if (session?[SessionUserName] != null)
                    return session[SessionUserName].ToString();

                TryRestoreFromCookie();
                return HttpContext.Current?.Session?[SessionUserName]?.ToString();
            }
        }

        public static string CurrentUserFullName
        {
            get
            {
                var session = HttpContext.Current?.Session;
                if (session?[SessionUserFullName] != null)
                    return session[SessionUserFullName].ToString();

                TryRestoreFromCookie();
                return HttpContext.Current?.Session?[SessionUserFullName]?.ToString() ?? "User";
            }
        }

        public static string CurrentUserEmail
        {
            get
            {
                var session = HttpContext.Current?.Session;
                if (session?[SessionUserEmail] != null)
                    return session[SessionUserEmail].ToString();

                TryRestoreFromCookie();
                return HttpContext.Current?.Session?[SessionUserEmail]?.ToString();
            }
        }

        public static string CurrentUserRole
        {
            get
            {
                var session = HttpContext.Current?.Session;
                if (session?[SessionUserRole] != null)
                    return session[SessionUserRole].ToString();

                TryRestoreFromCookie();
                return HttpContext.Current?.Session?[SessionUserRole]?.ToString();
            }
        }

        public static User GetCurrentUser()
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return null;

            return MongoDbContext.Instance.Users.Find(u => u.Id == userId).FirstOrDefault();
        }

        public static bool IsInRole(string role)
        {
            return string.Equals(CurrentUserRole, role, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryRestoreFromCookie()
        {
            try
            {
                var cookie = HttpContext.Current?.Request.Cookies[FormsAuthentication.FormsCookieName];
                if (cookie == null || string.IsNullOrEmpty(cookie.Value))
                    return false;

                var ticket = FormsAuthentication.Decrypt(cookie.Value);
                if (ticket == null || ticket.Expired)
                    return false;

                var parts = ticket.UserData?.Split('|');
                if (parts != null && parts.Length >= 3)
                {
                    var session = HttpContext.Current?.Session;
                    if (session != null)
                    {
                        session[SessionUserId] = parts[0];
                        session[SessionUserRole] = parts[1];
                        session[SessionUserFullName] = parts[2];
                        session[SessionUserEmail] = ticket.Name;
                    }
                    return true;
                }
            }
            catch
            {
                // Cookie corrupt or expired
            }
            return false;
        }

        public static string GenerateApiToken(User user)
        {
            var payload = $"{user.Id}|{user.Email}|{user.Role}|{DateTime.UtcNow.AddDays(7).Ticks}";
            var key = Encoding.UTF8.GetBytes("SmartCampusPortalSecretKey2026!#");
            using (var hmac = new HMACSHA256(key))
            {
                var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
                var tokenData = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
                return $"{tokenData}.{hash}";
            }
        }

        public static User ValidateApiToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            try
            {
                var parts = token.Split('.');
                if (parts.Length != 2) return null;

                var payload = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
                var hash = parts[1];

                var key = Encoding.UTF8.GetBytes("SmartCampusPortalSecretKey2026!#");
                using (var hmac = new HMACSHA256(key))
                {
                    var expectedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
                    if (hash != expectedHash) return null;
                }

                var data = payload.Split('|');
                if (data.Length < 4) return null;

                var expiryTicks = long.Parse(data[3]);
                if (DateTime.UtcNow.Ticks > expiryTicks) return null;

                var userId = data[0];
                return MongoDbContext.Instance.Users.Find(u => u.Id == userId && u.IsActive).FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}
