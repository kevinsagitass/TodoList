using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TodoList.Models;

namespace TodoList.Controllers
{
    public class TodoModelsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TodoModelsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TodoModels
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) {
                return NotFound();
            }

            List<TodoModel> todoList = await _context.Todo.Where(x => x.UserId == userId && x.deleted_at == null).ToListAsync();

            Todo[] todoModelList = new Todo[todoList.Count];

            for(var i = 0; i < todoList.Count; i++)
            {
                todoModelList[i] = new Todo { Id = todoList.ElementAt(i).Id, todoName = todoList.ElementAt(i).todoName, Priority = todoList.ElementAt(i).Priority };
            }

            return View(new TodoViewModel { todos = todoModelList });
        }

        // GET: TodoModels/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var todoModel = await _context.Todo
                .FirstOrDefaultAsync(m => m.Id == id);
            if (todoModel == null)
            {
                return NotFound();
            }
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Redirect("/Auth/Login");
            }
            else if (todoModel.UserId != userId)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                Response.ContentType = "application/json";
                await Response.WriteAsync("Unauthorized");
            }

            return View(todoModel);
        }

        // GET: TodoModels/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TodoModels/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,UserId,todoName,Priority")] TodoModel todoModel)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                todoModel.UserId = Convert.ToInt32(userId);
            }else
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                _context.Add(todoModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(todoModel);
        }

        // GET: TodoModels/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var todoModel = await _context.Todo.FindAsync(id);
            if (todoModel == null)
            {
                return NotFound();
            }
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Redirect("/Auth/Login");
            }else if(todoModel.UserId != userId)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                Response.ContentType = "application/json";
                await Response.WriteAsync("Unauthorized");
            }
            return View(todoModel);
        }

        // POST: TodoModels/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,todoName,Priority")] TodoModel todoModel)
        {
            if (id != todoModel.Id)
            {
                return NotFound();
            }
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return BadRequest();
            }

            todoModel.UserId = Convert.ToInt32(userId);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(todoModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TodoModelExists(todoModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(todoModel);
        }

        // GET: TodoModels/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var todoModel = await _context.Todo
                .FirstOrDefaultAsync(m => m.Id == id);
            if (todoModel == null)
            {
                return NotFound();
            }
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Redirect("/Auth/Login");
            }
            else if (todoModel.UserId != userId)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                Response.ContentType = "application/json";
                await Response.WriteAsync("Unauthorized");
            }

            return View(todoModel);
        }

        // POST: TodoModels/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var todoModel = await _context.Todo.FindAsync(id);
            _context.Todo.Remove(todoModel);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TodoModelExists(int id)
        {
            return _context.Todo.Any(e => e.Id == id);
        }

        [HttpPost]
        public async Task<IActionResult> DoneTodo(TodoViewModel todoViewModel)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Redirect("/Auth/Login");
            }

            List<int> idList = new List<int>();

            for(var i = 0; i < todoViewModel.todos.Length; i++)
            {
                if(todoViewModel.todos[i].Checked)
                {
                    idList.Add(todoViewModel.todos[i].Id);
                }
            }

            UserModel user = _context.UserModel.Find(userId);
            if(user.doneTodo == null)
            {
                user.doneTodo = 0;
            }
            user.doneTodo = user.doneTodo + idList.Count;

            _context.UserModel.Update(user);
            await _context.SaveChangesAsync();

            foreach(var id in idList)
            {
                _context.doneTodos.Add(new Models.DoneTodo { UserId = Convert.ToInt32(userId), todoId = id });
                await _context.SaveChangesAsync();

                TodoModel todo = _context.Todo.Find(id);
                todo.deleted_at = DateTime.Now;

                _context.Todo.Update(todo);
                await _context.SaveChangesAsync();
            }

            return Redirect("/Auth/Profile");
        }
    }
}
