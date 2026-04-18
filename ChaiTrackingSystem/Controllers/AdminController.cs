using ChaiTrackingSystem.DAL;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace ChaiTrackingSystem.Controllers
{
    public class AdminController : Controller
    {
        DBHelper db = new DBHelper();

        // 🔹 ADMIN CHECK
        private bool IsAdmin()
        {
            return Session["Role"] != null && Session["Role"].ToString() == "Admin";
        }

        // 🔹 DASHBOARD
        public ActionResult Dashboard()
        {
            if (!IsAdmin())
                return RedirectToAction("UserLogin", "Login");

            DataTable dt = db.GetAllRequests();
            return View(dt);
        }

        // 🔹 STATS
        public ActionResult Stats()
        { 
            DBHelper db = new DBHelper();

            string query = @"
                    SELECT r.*, u.Name 
                    FROM Requests r
                    INNER JOIN Users u ON r.UserId = u.Id
                    ORDER BY r.CreatedAt DESC";

            DataTable dt = db.GetData(query);

            return View(dt);
        }

        // 🔹 COMPLETE REQUEST
        public ActionResult Complete(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("UserLogin", "Login");

            string query = "UPDATE Requests SET Status='Completed' WHERE Id=@id";

            SqlParameter[] param = {
                new SqlParameter("@id", id)
            };

            db.Execute(query, param);

            TempData["msg"] = "Request completed!";
            return RedirectToAction("Dashboard");
        }

        // 🔹 TOP USERS
        public ActionResult TopUsers()
        {
            if (!IsAdmin())
                return RedirectToAction("UserLogin", "Login");

            string query = @"
                SELECT TOP 5 U.Name, COUNT(*) AS Orders, SUM(R.Cups) AS TotalCups
                FROM Requests R
                JOIN Users U ON R.UserId = U.Id
                GROUP BY U.Name
                ORDER BY TotalCups DESC";

            return View(db.GetData(query, null));
        }

        // 🔹 ADD ADMIN (GET)
        public ActionResult AddAdmin()
        {
            if (!IsAdmin())
                return RedirectToAction("UserLogin", "Login");

            return View();
        }

        // 🔹 ADD ADMIN (POST)
        [HttpPost]
        public ActionResult AddAdmin(string name, string email, string password)
        {
            if (!IsAdmin())
                return RedirectToAction("UserLogin", "Login");

            string query = @"INSERT INTO Users(Name,Email,Password,Department,Role) 
                             VALUES(@n,@e,@p,'IT','Admin')";

            SqlParameter[] param = {
                new SqlParameter("@n", name),
                new SqlParameter("@e", email),
                new SqlParameter("@p", password)
            };

            db.Execute(query, param);

            TempData["msg"] = "New Admin Added Successfully!";
            return RedirectToAction("Dashboard");
        }
        //DepartmentStats
        public ActionResult DepartmentStats()
        {
                string query = @"
            SELECT u.Department, 
                   COUNT(r.Id) AS TotalRequests,
                   SUM(r.Cups) AS TotalCups
                   FROM Requests r
                   INNER JOIN Users u ON r.UserId = u.Id
                    GROUP BY u.Department
                ";

            DBHelper db = new DBHelper();
            var data = db.GetData(query);

            return View(data); // 👈 DepartmentStats.cshtml
        }
       
        // 🔹 PEAK TIME
        public ActionResult PeakTime()
        {
            if (!IsAdmin())
                return RedirectToAction("UserLogin", "Login");

            string query = @"
                SELECT DATEPART(HOUR, CreatedAt) AS Hour, COUNT(*) AS Total
                FROM Requests
                GROUP BY DATEPART(HOUR, CreatedAt)
                ORDER BY Total DESC";

            return View(db.GetData(query, null));
        }

        // 🔹 EXPORT PDF
        public FileResult DownloadPDF()
        {
            if (!IsAdmin())
                return null;

            DataTable dt = db.GetAllRequests();

            using (var ms = new System.IO.MemoryStream())
            {
                var doc = new iTextSharp.text.Document();
                iTextSharp.text.pdf.PdfWriter.GetInstance(doc, ms);

                doc.Open();

                foreach (DataRow row in dt.Rows)
                {
                    string line = $"Drink: {row["DrinkType"]}, Cups: {row["Cups"]}, Status: {row["Status"]}";
                    doc.Add(new iTextSharp.text.Paragraph(line));
                }

                doc.Close();

                return File(ms.ToArray(), "application/pdf", "Download.pdf");
            }
        }
    }
}