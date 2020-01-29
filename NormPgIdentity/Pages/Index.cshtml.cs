using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Norm.Extensions;

namespace NormPgIdentity.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly DbConnection _connection;

        public IndexModel(ILogger<IndexModel> logger, DbConnection connection)
        {
            _logger = logger;
            _connection = connection;
        }

        public IAsyncEnumerable<(int id, string value)> Values { get; private set; }

        public void OnGet()
        {
            Values = _connection.ReadAsync<int, string>("select id, string from random_strings");
        }
    }
}
