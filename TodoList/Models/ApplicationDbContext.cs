using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TodoList.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options): base(options)
        {

        }
        public DbSet<TodoModel> Todo { get; set; }

        public DbSet<UserModel> UserModel { get; set; }

        public DbSet<DoneTodo> doneTodos { get; set; }
    }
}
