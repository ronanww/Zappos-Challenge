using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zappos
{
    public class UserRequest
    {
        public string email { get; set; }
        public string product { get; set; }
        public bool sent { get; set; }
        public int lastDiscount { get; set; }
    }
}
