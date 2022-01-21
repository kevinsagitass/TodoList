using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TodoList.Models
{
    public class DoneTodoViewModel
    {
        public int UserId { get; set; }
        public string todoName { get; set; }
        public DateTime? doneDate { get; set; }
    }
}
