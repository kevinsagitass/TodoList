using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TodoList.Models;

namespace TodoList.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        public static readonly string key = "E546C8DF278CD5931069B522E695D4F2";

        public AuthController(ApplicationDbContext context)
        {
            this._context = context;
        }
        // GET: Auth
        public ActionResult Index()
        {
            return Redirect("/Login");
        }

        // GET: Auth/Login
        public ActionResult Login(string msg = "")
        {
            ViewBag.Msg = msg == "user_not_found" ? "User Not Found" : "";
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                return Redirect("/Home");
            }
            return View();
        }

        // GET: Auth/Register
        public ActionResult Register()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                return Redirect("/Home");
            }
            return View();
        }

        // POST: Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login([Bind("Email,Password,Remember")] UserViewModel userModel)
        {
            try
            {
                UserModel user = _context.UserModel.Where(x => x.Email == userModel.Email).FirstOrDefault();

                if(user == null)
                {
                    return RedirectToAction("Login", "Auth", new { msg = "user_not_found" });
                }
                else
                {
                    byte[] hashBytes = Convert.FromBase64String(user.Password);
                    byte[] salt = new byte[16];
                    Array.Copy(hashBytes, 0, salt, 0, 16);

                    var pbkdf2 = new Rfc2898DeriveBytes(userModel.Password, salt, 100000);
                    byte[] hash = pbkdf2.GetBytes(20);

                    for (int i = 0; i < 20; i++)
                        if (hashBytes[i + 16] != hash[i])
                            return RedirectToAction("Login", "Auth", new { msg = "user_not_found" });

                    if (userModel.Remember)
                    {
                        var encryptedId = EncryptString(Convert.ToString(user.UserId), key);

                        Response.Cookies.Append("TodoList.UserId", encryptedId, new CookieOptions { Expires = DateTime.Now.AddDays(1) });
                    }

                    HttpContext.Session.SetInt32("UserId", user.UserId);

                    return Redirect("/TodoModels");
                }                
            }
            catch
            {
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register([Bind("Email,Password")] UserModel userModel)
        {
            try
            {
                if(ModelState.IsValid)
                {
                    byte[] salt;
                    new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

                    var pbkdf2 = new Rfc2898DeriveBytes(userModel.Password, salt, 100000);
                    byte[] hash = pbkdf2.GetBytes(20);

                    byte[] hashBytes = new byte[36];
                    Array.Copy(salt, 0, hashBytes, 0, 16);
                    Array.Copy(hash, 0, hashBytes, 16, 20);

                    string savedPasswordHash = Convert.ToBase64String(hashBytes);

                    userModel.Password = savedPasswordHash;

                    _context.UserModel.Add(userModel);
                    await _context.SaveChangesAsync();

                    int userId = userModel.UserId;
                    HttpContext.Session.SetInt32("UserId", userId);

                    return Redirect("/TodoModels");
                }
            } catch
            {
                return View();
            }

            return View(userModel);
        }

        public ActionResult Logout()
        {
            try
            {
                HttpContext.Session.Remove("UserId");

                if(Request.Cookies["TodoList.UserId"] != null)
                {
                    Response.Cookies.Delete("TodoList.UserId");
                }

                return Redirect("/Home");
            }
            catch
            {
                return View();
            }
        }

        public ActionResult Profile(string msg = "")
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Redirect("/Auth/Login");
            }
            UserModel user = _context.UserModel.Find(userId);
            if(user == null)
            {
                return Redirect("/Auth/Login");
            }
            List<DoneTodoViewModel> doneTodos = _context.doneTodos.Join(
                    _context.Todo,
                    done => done.todoId,
                    todo => todo.Id,
                    (done, todo) => new DoneTodoViewModel
                    {
                        UserId = todo.UserId,
                        todoName = todo.todoName,
                        doneDate = todo.deleted_at,
                    }
                ).Where(x => x.UserId == userId).ToList();
            ViewBag.user = user;
            ViewBag.doneTodos = doneTodos;
            ViewBag.fileMsg = msg == "file_not_found" ? "No File Selected" : "";
            ViewBag.fileMsg = msg == "file_not_valid" ? "Please Use Valid File Extensions (Jpg, Png)" : "";
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> SaveImage(IFormFile file)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if(userId == null)
            {
                return Redirect("/Auth/Login");
            }

            if(file == null || file.Length == 0)
            {
                return RedirectToAction("Profile", "Auth", new { msg = "file_not_found" });
            }

            string[] allowedExtension = new string[] { ".jpg", ".png" };

            var extension = Path.GetExtension(file.FileName);
            if(!allowedExtension.Contains(extension))
            {
                return RedirectToAction("Profile", "Auth", new { msg = "file_not_valid" });
            }

            var path = Path.Combine(
                  Directory.GetCurrentDirectory(), "wwwroot", "Image",
                  file.FileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            UserModel user = _context.UserModel.Find(userId);
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Image", user.userProfilePic);
            user.userProfilePic = file.FileName;

            if (System.IO.File.Exists(oldPath))
            {
                System.IO.File.Delete(oldPath);
            }

            _context.UserModel.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Profile");
        }

        public string EncryptString(string text, string keyString)
        {
            var key = Encoding.UTF8.GetBytes(keyString);

            using (var aesAlg = Aes.Create())
            {
                using (var encryptor = aesAlg.CreateEncryptor(key, aesAlg.IV))
                {
                    using (var msEncrypt = new MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(text);
                        }

                        var iv = aesAlg.IV;

                        var decryptedContent = msEncrypt.ToArray();

                        var result = new byte[iv.Length + decryptedContent.Length];

                        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                        Buffer.BlockCopy(decryptedContent, 0, result, iv.Length, decryptedContent.Length);

                        return Convert.ToBase64String(result);
                    }
                }
            }
        }

        public static string DecryptString(string cipherText, string keyString)
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            var iv = new byte[16];
            var cipher = new byte[16];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, iv.Length);
            var key = Encoding.UTF8.GetBytes(keyString);

            using (var aesAlg = Aes.Create())
            {
                using (var decryptor = aesAlg.CreateDecryptor(key, iv))
                {
                    string result;
                    using (var msDecrypt = new MemoryStream(cipher))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                result = srDecrypt.ReadToEnd();
                            }
                        }
                    }

                    return result;
                }
            }
        }
    }
}