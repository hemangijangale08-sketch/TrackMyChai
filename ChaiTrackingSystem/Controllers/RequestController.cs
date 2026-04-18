using ChaiTrackingSystem.DAL;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Web.Mvc;

namespace ChaiTrackingSystem.Controllers
{
    public class RequestController : Controller
    {
        DBHelper db = new DBHelper();

        // ✅ USER DASHBOARD
        public ActionResult Dashboard()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");
      

            int userId = Convert.ToInt32(Session["UserId"]);

            string query = @"
                SELECT r.*, u.Name 
                FROM Requests r
                INNER JOIN Users u ON r.UserId = u.Id
                WHERE r.UserId = @uid
                ORDER BY r.CreatedAt DESC
            ";

            SqlParameter[] param = {
                new SqlParameter("@uid", userId)
            };

            DataTable dt = db.GetData(query, param);

            return View(dt);
        }

        // ✅ ADMIN DASHBOARD
        public ActionResult AdminDashboard(string search)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            string query;
            SqlParameter[] param = null;

            if (!string.IsNullOrEmpty(search))
            {
                query = @"
            SELECT r.*, u.Name
            FROM Requests r
            INNER JOIN Users u ON r.UserId = u.Id
            WHERE u.Name LIKE @search
            ORDER BY r.CreatedAt DESC";

                param = new SqlParameter[]
                {
            new SqlParameter("@search", "%" + search + "%")
                };
            }
            else
            {
                query = @"
            SELECT r.*, u.Name
            FROM Requests r
            INNER JOIN Users u ON r.UserId = u.Id
            ORDER BY r.CreatedAt DESC";
            }

            DataTable dt = db.GetData(query, param);

            return View(dt);
        }

        // ✅ CREATE (GET)
        public ActionResult Create()
        {
            return View();
        }

        // ✅ CREATE (POST)
        [HttpPost]
        public ActionResult Create(string drinkType, int cups)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            int userId = Convert.ToInt32(Session["UserId"]);

            int price = 0;

            if (drinkType == "Tea")
                price = cups * 10;
            else if (drinkType == "Coffee")
                price = cups * 20;

            string query = @"INSERT INTO Requests(UserId,DrinkType,Cups,Price,Status,CreatedAt)
                     VALUES(@u,@d,@c,@p,'Pending',GETDATE())";

            SqlParameter[] param = {
        new SqlParameter("@u", userId),
        new SqlParameter("@d", drinkType),
        new SqlParameter("@c", cups),
        new SqlParameter("@p", price)
    };

            db.Execute(query, param);

            TempData["msg"] = "Chai Request Added!";
            return RedirectToAction("Dashboard");
        }

        // ✅ EDIT (GET)
        public ActionResult Edit(int id)
        {
            string query = "SELECT * FROM Requests WHERE Id=@id";

            SqlParameter[] param = {
                new SqlParameter("@id", id)
            };

            DataTable dt = db.GetData(query, param);

            return View(dt);
        }

        // ✅ EDIT (POST)
        [HttpPost]
        public ActionResult Edit(int id, string drinkType, int cups)
        {
            string query = "UPDATE Requests SET DrinkType=@d, Cups=@c WHERE Id=@id";

            SqlParameter[] param = {
                new SqlParameter("@d", drinkType),
                new SqlParameter("@c", cups),
                new SqlParameter("@id", id)
            };

            db.Execute(query, param);

            return RedirectToAction("AdminDashboard");
        }

        // ✅ DELETE
        public ActionResult Delete(int id)
        {
            string query = "DELETE FROM Requests WHERE Id=@id";

            SqlParameter[] param = {
                new SqlParameter("@id", id)
            };

            db.Execute(query, param);

            return RedirectToAction("AdminDashboard");
        }

        // ✅ COMPLETE
        public ActionResult Complete(int id)
        {
            string query = "UPDATE Requests SET Status='Completed' WHERE Id=@id";

            SqlParameter[] param = {
                new SqlParameter("@id", id)
            };

            db.Execute(query, param);

            return RedirectToAction("AdminDashboard");
        }

        // ✅ HISTORY
        public ActionResult History(string type)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            int userId = Convert.ToInt32(Session["UserId"]);
            string query = "";

            if (type == "daily")
            {
                query = @"SELECT DrinkType, SUM(Cups) AS Total 
                          FROM Requests 
                          WHERE UserId=@uid AND CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)
                          GROUP BY DrinkType";
            }
            else if (type == "weekly")
            {
                query = @"SELECT DrinkType, SUM(Cups) AS Total 
                          FROM Requests 
                          WHERE UserId=@uid AND DATEPART(WEEK, CreatedAt) = DATEPART(WEEK, GETDATE())
                          GROUP BY DrinkType";
            }
            else
            {
                query = @"SELECT DrinkType, SUM(Cups) AS Total 
                          FROM Requests 
                          WHERE UserId=@uid AND MONTH(CreatedAt) = MONTH(GETDATE())
                          GROUP BY DrinkType";
            }

            SqlParameter[] param = {
                new SqlParameter("@uid", userId)
            };

            DataTable dt = db.GetData(query, param);

            ViewBag.Type = type;
            return View(dt);
        }

        public ActionResult MyBill()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            if (Session["Role"].ToString() != "User")
                return RedirectToAction("Login", "Account");

            int userId = Convert.ToInt32(Session["UserId"]);

            string query = "SELECT * FROM Requests WHERE UserId=@id";

            SqlParameter[] param = {
        new SqlParameter("@id", userId)
    };

            DataTable dt = db.GetData(query, param);

            return View(dt);
        }

        public ActionResult AdminReport()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            string query = @"SELECT U.Name, R.*
                     FROM Requests R
                     JOIN Users U ON R.UserId = U.Id
                     ORDER BY R.CreatedAt DESC";

            DataTable dt = db.GetData(query);

            return View(dt);
        }


        public ActionResult DownloadPDF()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            int userId = Convert.ToInt32(Session["UserId"]);
            string role = Session["Role"].ToString();

            DBHelper db = new DBHelper();
            DataTable dt;

            if (role == "User")
            {
                string query = @"SELECT u.Name, r.DrinkType, r.Cups, r.Price, r.CreatedAt 
                         FROM Requests r 
                         INNER JOIN Users u ON r.UserId = u.Id
                         WHERE r.UserId = @uid
                         ORDER BY r.CreatedAt DESC";

                SqlParameter[] param = {
            new SqlParameter("@uid", userId)
        };

                dt = db.GetData(query, param);
            }
            else
            {
                string query = @"SELECT u.Name, r.DrinkType, r.Cups, r.Price, r.CreatedAt 
                         FROM Requests r 
                         INNER JOIN Users u ON r.UserId = u.Id
                         ORDER BY r.CreatedAt DESC";

                dt = db.GetData(query);
            }

            MemoryStream ms = new MemoryStream();
            Document doc = new Document(PageSize.A4, 30, 30, 30, 30);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            // 🎨 COLORS
            BaseColor primary = new BaseColor(230, 126, 34);
            BaseColor lightGray = new BaseColor(245, 245, 245);

            // 🔥 HEADER (Logo + Name + Invoice Info)
            PdfPTable header = new PdfPTable(2);
            header.WidthPercentage = 100;
            header.SetWidths(new float[] { 3, 2 });

            // LEFT SIDE (Logo + Name)
            PdfPCell leftCell = new PdfPCell();
            leftCell.Border = 0;

            string logoPath = Server.MapPath("~/Content/chai-logo.jpg");
            if (System.IO.File.Exists(logoPath))
            {
                Image logo = Image.GetInstance(logoPath);
                logo.ScaleToFit(60f, 60f);
                leftCell.AddElement(logo);
            }

            Paragraph appName = new Paragraph("",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20, primary));
            leftCell.AddElement(appName);

            header.AddCell(leftCell);

            // RIGHT SIDE (Invoice Info)
            PdfPCell rightCell = new PdfPCell();
            rightCell.Border = 0;

            rightCell.AddElement(new Paragraph("Invoice",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16)));

            rightCell.AddElement(new Paragraph("Date: " + DateTime.Now.ToString("dd-MM-yyyy")));
            rightCell.AddElement(new Paragraph("Invoice No: #" + DateTime.Now.Ticks.ToString().Substring(10)));

            rightCell.HorizontalAlignment = Element.ALIGN_RIGHT;

            header.AddCell(rightCell);

            doc.Add(header);

            doc.Add(new Paragraph("\n"));

            // 👤 USER INFO
            string userName = dt.Rows.Count > 0 ? dt.Rows[0]["Name"].ToString() : "User";

            Paragraph userInfo = new Paragraph("Billed To: " + userName,
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12));

            doc.Add(userInfo);
            doc.Add(new Paragraph("\n"));

            // 📊 TABLE
            PdfPTable table = new PdfPTable(5);
            table.WidthPercentage = 100;

            string[] cols = { "Drink", "Cups", "Price", "Date", "Status" };

            foreach (var col in cols)
            {
                PdfPCell cell = new PdfPCell(new Phrase(col));
                cell.BackgroundColor = primary;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.Padding = 6;
                cell.BorderColor = BaseColor.WHITE;
                table.AddCell(cell);
            }

            int total = 0;

            foreach (DataRow row in dt.Rows)
            {
                table.AddCell(row["DrinkType"].ToString());
                table.AddCell(row["Cups"].ToString());
                table.AddCell("₹ " + row["Price"].ToString());
                table.AddCell(Convert.ToDateTime(row["CreatedAt"]).ToString("dd-MM-yyyy"));
                table.AddCell("Completed");

                total += Convert.ToInt32(row["Price"]);
            }

            doc.Add(table);

            doc.Add(new Paragraph("\n"));

            // 💰 TOTAL BOX
            PdfPTable totalTable = new PdfPTable(2);
            totalTable.WidthPercentage = 40;
            totalTable.HorizontalAlignment = Element.ALIGN_RIGHT;

            PdfPCell t1 = new PdfPCell(new Phrase("Total"));
            t1.BackgroundColor = lightGray;
            t1.Padding = 8;

            PdfPCell t2 = new PdfPCell(new Phrase("₹ " + total,
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
            t2.BackgroundColor = lightGray;
            t2.Padding = 8;

            totalTable.AddCell(t1);
            totalTable.AddCell(t2);

            doc.Add(totalTable);

            doc.Add(new Paragraph("\n\n"));

            // ❤️ FOOTER
            Paragraph footer = new Paragraph(
                "Thank you for Visiting ☕\nEnjoy your chai breaks!",
                FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 10));

            footer.Alignment = Element.ALIGN_CENTER;

            doc.Add(footer);

            doc.Close();

            return File(ms.ToArray(), "application/pdf", "Invoice.pdf");
        }
    }
}