using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TodoList.Models
{
    public class TodoViewModel
    {
        public Todo[] todos { get; set; }
    }

    public class Todo
    {
        public int Id { get; set; }
        public string todoName { get; set; }
        public int Priority { get; set; }
        public bool Checked { get; set; }
    }
}
