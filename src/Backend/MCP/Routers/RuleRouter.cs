using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Backend.Persistence.Interfaces;
using Backend.Persistence.Models;

namespace Backend.MCP.Routers
{
    public class RuleRouter : IMCPRouter
    {
        private readonly IRepository<Card> _repository;

        public RuleRouter(IRepository<Card> repository)
        {
            _repository = repository;
        }

        public bool CanHandle(string query)
        {
            // Simple rules: "count", "list", "show"
            return Regex.IsMatch(query, @"\b(count|list|show|find)\b", RegexOptions.IgnoreCase);
        }

        public async Task<string> ProcessRequestAsync(string query)
        {
            var cards = await _repository.GetAllAsync();

            if (Regex.IsMatch(query, @"count", RegexOptions.IgnoreCase))
            {
                return $"Total cards: {cards.Count()}";
            }
            
            if (Regex.IsMatch(query, @"find|show", RegexOptions.IgnoreCase))
            {
                // Extract name
                var match = Regex.Match(query, @"(find|show)\s+(.*)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var term = match.Groups[2].Value.Trim();
                    var found = cards.Where(c => c.Name.Contains(term, System.StringComparison.OrdinalIgnoreCase)).Take(5);
                    if (!found.Any()) return "No cards found.";
                    return "Found: " + string.Join(", ", found.Select(c => c.Name));
                }
            }

            return "I understood the command but couldn't execute specific logic.";
        }
    }
}
