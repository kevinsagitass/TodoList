using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TodoList.Models
{
    public class UserModel
    {
        [Key]
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string userProfilePic { get; set; }
        public virtual ICollection<TodoModel> Todos { get; set; }
        public int doneTodo { get; set; }
    }
}
