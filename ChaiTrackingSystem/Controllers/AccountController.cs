using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using ChaiTrackingSystem.DAL;

namespace ChaiTrackingSystem.Controllers
{
    public class AccountController : Controller
    {
        DBHelper db = new DBHelper();

        // ✅ REGISTER
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(string name, string email, string password, string dept)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "All fields are required";
                return View();
            }

            // Check Email Exists
            string checkQuery = "SELECT * FROM Users WHERE Email=@e";
            SqlParameter[] checkParam = {
                new SqlParameter("@e", email)
            };

            DataTable dt = db.GetData(checkQuery, checkParam);

            if (dt.Rows.Count > 0)
            {
                ViewBag.Error = "Email already exists!";
                return View();
            }

            // ✅ Default role = User
            string query = @"INSERT INTO Users(Name,Email,Password,Department,Role) 
                             VALUES(@n,@e,@p,@d,'User')";

            SqlParameter[] param = {
                new SqlParameter("@n", name),
                new SqlParameter("@e", email),
                new SqlParameter("@p", password),
                new SqlParameter("@d", dept)
            };

            db.Execute(query, param);

            TempData["Success"] = "Registration successful!";
            return RedirectToAction("Login");
        }

        // ✅ LOGIN GET
        public ActionResult Login()
        {
            return View();
        }

        // ✅ LOGIN POST (ONLY ONE METHOD)
        [HttpPost]
        public ActionResult Login(string email, string password)
        {
            DBHelper db = new DBHelper();
            var dt = db.GetUser(email, password);

            if (dt.Rows.Count > 0)
            {
                Session["UserId"] = dt.Rows[0]["Id"];
                Session["Role"] = dt.Rows[0]["Role"].ToString();

                if (Session["Role"].ToString() == "Admin")
                    return RedirectToAction("AdminDashboard", "Request");
                else
                    return RedirectToAction("Dashboard", "Request");
            }

            ViewBag.Error = "Invalid Login";
            return View();
        }

        // ✅ LOGOUT
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}