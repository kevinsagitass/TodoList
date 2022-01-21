using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace TodoList.Models
{
    public class TodoModel
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        [Required]
        [Display(Name = "Todo Name")]
        public string todoName { get; set; }
        [Range(1, 5)]
        public int Priority { get; set; }
        public virtual UserModel user { get; set; }
        public DateTime? deleted_at { get; set; }
    }
}
