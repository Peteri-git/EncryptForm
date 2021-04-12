using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace EncryptForm.Server.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string PublicKey { get; set; }
    }
}
