using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TodoList.Models
{
    public class DoneTodo
    {
        [Key]
        public int DoneTodoId { get; set; }
        public int UserId { get; set; }
        public int todoId { get; set; }
    }
}
