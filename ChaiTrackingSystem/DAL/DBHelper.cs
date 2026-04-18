using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace ChaiTrackingSystem.DAL
{
    public class DBHelper
    {
        private readonly string conStr;

        public DBHelper()
        {
            conStr = ConfigurationManager.ConnectionStrings["conStr"].ConnectionString;
        }

        // ✅ GET CONNECTION
        private SqlConnection GetConnection()
        {
            return new SqlConnection(conStr);
        }

        // ✅ GET DATA (SELECT)
        public DataTable GetData(string query, SqlParameter[] param = null)
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 30; // 🔥 timeout fix

                    if (param != null)
                        cmd.Parameters.AddRange(param);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            return dt;
        }

        // ✅ INSERT / UPDATE / DELETE
        public int Execute(string query, SqlParameter[] param = null)
        {
            using (SqlConnection con = GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 30;

                    if (param != null)
                        cmd.Parameters.AddRange(param);

                    con.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        // ✅ USER LOGIN (IMPORTANT FIX)
        public DataTable GetUser(string email, string password)
        {
            string query = @"SELECT Id, Name, Email, Role 
                             FROM Users 
                             WHERE Email=@Email AND Password=@Password";

            SqlParameter[] param = {
                new SqlParameter("@Email", email),
                new SqlParameter("@Password", password)
            };

            return GetData(query, param);
        }

        // ✅ USER REQUESTS
        public DataTable GetUserRequests(int userId)
        {
            string query = @"SELECT * 
                             FROM Requests 
                             WHERE UserId=@UserId
                             ORDER BY CreatedAt DESC";

            SqlParameter[] param = {
                new SqlParameter("@UserId", userId)
            };

            return GetData(query, param);
        }

        // ✅ ADMIN - ALL REQUESTS WITH USER NAME 🔥 (IMPORTANT FIX)
        public DataTable GetAllRequests()
        {
            string query = @"
                SELECT 
                    r.Id,
                    u.Name,
                    r.DrinkType,
                    r.Cups,
                    r.Status,
                    r.CreatedAt
                FROM Requests r
                INNER JOIN Users u ON r.UserId = u.Id
                ORDER BY r.CreatedAt DESC";

            return GetData(query);
        }
    }
}