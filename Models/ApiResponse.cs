using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sport_app_backend.Models
{
    public class ApiResponse
    {
        public required bool Action { get; set; } = false;
        public required string Message { get; set; } = string.Empty;

        // generics VARIBALE
        public Object? Result { get; set; }
        }
}