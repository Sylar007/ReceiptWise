namespace ReceiptWise.Services.Business;

using Microsoft.Extensions.Logging;
using ReceiptWise.Core.Enums;

/// <summary>
/// Rule-based engine for categorizing receipts based on merchant name and keywords
/// Fast, offline, deterministic categorization for common merchants
/// </summary>
public class CategoryMappingEngine
{
    private readonly ILogger<CategoryMappingEngine>? _logger;
    private readonly Dictionary<ReceiptCategory, List<string>> _merchantKeywords;

    public CategoryMappingEngine(ILogger<CategoryMappingEngine>? logger = null)
    {
        _logger = logger;
        _merchantKeywords = InitializeKeywords();
    }

    /// <summary>
    /// Suggest category based on merchant name using keyword matching
    /// Returns null if no confident match found
    /// </summary>
    public ReceiptCategory? SuggestCategory(string merchantName, IEnumerable<string>? itemDescriptions = null)
    {
        if (string.IsNullOrWhiteSpace(merchantName))
        {
            _logger?.LogDebug("Empty merchant name, cannot categorize");
            return null;
        }

        var normalizedMerchant = merchantName.ToLowerInvariant().Trim();
        _logger?.LogDebug("Attempting to categorize merchant: {Merchant}", merchantName);

        // Try exact merchant matching first
        var category = MatchByMerchantName(normalizedMerchant);
        if (category.HasValue)
        {
            _logger?.LogInformation("Matched merchant '{Merchant}' to category: {Category}",
                merchantName, category.Value);
            return category.Value;
        }

        // Try item-based matching if available
        if (itemDescriptions != null && itemDescriptions.Any())
        {
            category = MatchByItemDescriptions(itemDescriptions);
            if (category.HasValue)
            {
                _logger?.LogInformation("Matched items to category: {Category}", category.Value);
                return category.Value;
            }
        }

        _logger?.LogDebug("No rule-based match found for merchant: {Merchant}", merchantName);
        return null;
    }

    /// <summary>
    /// Match merchant name against keyword database
    /// </summary>
    private ReceiptCategory? MatchByMerchantName(string normalizedMerchant)
    {
        foreach (var (category, keywords) in _merchantKeywords)
        {
            foreach (var keyword in keywords)
            {
                if (normalizedMerchant.Contains(keyword.ToLowerInvariant()))
                {
                    return category;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Match based on item descriptions
    /// </summary>
    private ReceiptCategory? MatchByItemDescriptions(IEnumerable<string> itemDescriptions)
    {
        var normalizedItems = itemDescriptions
            .Select(i => i.ToLowerInvariant())
            .ToList();

        // Count matches per category
        var categoryScores = new Dictionary<ReceiptCategory, int>();

        foreach (var (category, keywords) in _merchantKeywords)
        {
            var matches = keywords.Count(keyword =>
                normalizedItems.Any(item => item.Contains(keyword.ToLowerInvariant())));

            if (matches > 0)
            {
                categoryScores[category] = matches;
            }
        }

        // Return category with most matches (if confident)
        if (categoryScores.Any())
        {
            var topCategory = categoryScores.OrderByDescending(x => x.Value).First();
            if (topCategory.Value >= 2) // Require at least 2 matching items
            {
                return topCategory.Key;
            }
        }

        return null;
    }

    /// <summary>
    /// Initialize comprehensive keyword database
    /// </summary>
    private Dictionary<ReceiptCategory, List<string>> InitializeKeywords()
    {
        return new Dictionary<ReceiptCategory, List<string>>
        {
            [ReceiptCategory.Groceries] = new List<string>
            {
                // Major chains
                "walmart", "target", "kroger", "safeway", "albertsons", "publix",
                "whole foods", "trader joe", "aldi", "costco", "sam's club",
                "food lion", "wegmans", "heb", "meijer", "giant eagle",
                "stop & shop", "harris teeter", "fred meyer", "qfc",
                
                // International
                "tesco", "sainsbury", "asda", "carrefour", "lidl",
                
                // Keywords
                "grocery", "supermarket", "market", "mart", "food store"
            },

            [ReceiptCategory.Dining] = new List<string>
            {
                // Fast Food
                "mcdonald", "burger king", "wendy", "taco bell", "kfc",
                "subway", "pizza hut", "domino", "papa john", "arby",
                "chick-fil-a", "popeyes", "chipotle", "panera", "sonic",
                
                // Casual Dining
                "applebee", "chili", "olive garden", "red lobster", "outback",
                "cheesecake factory", "buffalo wild", "texas roadhouse",
                
                // Coffee
                "starbucks", "dunkin", "peet", "caribou", "tim horton",
                "costa coffee",
                
                // Keywords
                "restaurant", "cafe", "coffee", "diner", "bistro", "grill",
                "bar & grill", "pizzeria", "deli", "bakery"
            },

            [ReceiptCategory.Transportation] = new List<string>
            {
                // Gas Stations
                "shell", "chevron", "exxon", "mobil", "bp", "valero",
                "marathon", "speedway", "circle k", "7-eleven", "wawa",
                "sunoco", "arco", "phillips 66", "conoco", "texaco",
                
                // Ride Share
                "uber", "lyft", "taxi", "cab",
                
                // Public Transit
                "metro", "transit", "subway", "bus", "train",
                
                // Keywords
                "gas station", "fuel", "parking", "toll", "car wash"
            },

            [ReceiptCategory.Shopping] = new List<string>
            {
                // Department Stores
                "macy", "nordstrom", "kohl", "jcpenney", "dillard",
                "bloomingdale", "neiman marcus", "saks",
                
                // Retail
                "amazon", "ebay", "walmart", "target", "best buy",
                "tjmaxx", "marshalls", "ross", "burlington",
                
                // Clothing
                "gap", "old navy", "h&m", "zara", "uniqlo", "forever 21",
                "victoria secret", "foot locker", "nike", "adidas",
                
                // Keywords
                "store", "shop", "retail", "boutique", "outlet", "mall"
            },

            [ReceiptCategory.Healthcare] = new List<string>
            {
                // Pharmacies
                "cvs", "walgreens", "rite aid", "duane reade", "pharmacy",
                
                // Medical
                "hospital", "clinic", "urgent care", "doctor", "dentist",
                "medical center", "health center", "vision center",
                "optometrist", "chiropractor", "physical therapy",
                
                // Keywords
                "rx", "prescription", "medicine", "health", "wellness"
            },

            [ReceiptCategory.Utilities] = new List<string>
            {
                // Utilities
                "electric", "power", "energy", "gas company", "water",
                "sewage", "waste management", "recycling",
                
                // Telecom
                "verizon", "at&t", "t-mobile", "sprint", "comcast",
                "xfinity", "spectrum", "cox", "internet", "cable",
                
                // Keywords
                "utility", "bill", "service provider"
            },

            [ReceiptCategory.Entertainment] = new List<string>
            {
                // Theaters
                "amc", "regal", "cinemark", "cinema", "theater", "theatre",
                
                // Streaming
                "netflix", "hulu", "disney", "spotify", "apple music",
                "amazon prime", "youtube premium",
                
                // Gaming
                "steam", "playstation", "xbox", "nintendo", "gamestop",
                
                // Events
                "ticketmaster", "stubhub", "concert", "event", "show",
                
                // Keywords
                "entertainment", "movie", "game", "music", "ticket"
            },

            [ReceiptCategory.Travel] = new List<string>
            {
                // Airlines
                "delta", "american airlines", "united", "southwest",
                "jetblue", "alaska airlines", "spirit", "frontier",
                
                // Hotels
                "marriott", "hilton", "hyatt", "holiday inn", "best western",
                "hampton inn", "courtyard", "radisson", "sheraton",
                "airbnb", "vrbo", "booking.com", "expedia", "hotels.com",
                
                // Car Rental
                "hertz", "enterprise", "avis", "budget", "national",
                
                // Keywords
                "hotel", "motel", "resort", "airline", "airport",
                "rental car", "travel", "vacation", "trip"
            },

            [ReceiptCategory.HomeAndGarden] = new List<string>
            {
                // Home Improvement
                "home depot", "lowe", "menards", "ace hardware",
                "true value", "harbor freight",
                
                // Furniture
                "ikea", "ashley", "wayfair", "pottery barn", "crate & barrel",
                "west elm", "rooms to go",
                
                // Garden
                "garden center", "nursery", "landscaping",
                
                // Keywords
                "hardware", "furniture", "home improvement", "garden",
                "lawn", "plant", "decor", "appliance"
            },

            [ReceiptCategory.Technology] = new List<string>
            {
                // Electronics
                "best buy", "apple store", "microsoft store", "dell",
                "hp", "lenovo", "samsung", "sony",
                
                // Online
                "amazon", "newegg", "b&h photo", "micro center",
                
                // Software
                "adobe", "microsoft 365", "office", "software",
                
                // Keywords
                "electronics", "computer", "laptop", "phone", "tablet",
                "tech", "digital", "gadget"
            },

            [ReceiptCategory.Services] = new List<string>
            {
                // Professional Services
                "lawyer", "attorney", "accountant", "tax", "insurance",
                "bank", "financial", "advisor",
                
                // Personal Services
                "salon", "barber", "spa", "gym", "fitness",
                "dry clean", "laundry", "tailor", "repair",
                
                // Home Services
                "plumber", "electrician", "hvac", "cleaning service",
                "lawn care", "pest control", "locksmith",
                
                // Keywords
                "service", "professional", "consultation", "fee"
            },

            [ReceiptCategory.Other] = new List<string>
            {
                // Catch-all
                "other", "misc", "miscellaneous", "general"
            }
        };
    }

    /// <summary>
    /// Get all keywords for a specific category (for testing/debugging)
    /// </summary>
    public IEnumerable<string> GetKeywordsForCategory(ReceiptCategory category)
    {
        return _merchantKeywords.TryGetValue(category, out var keywords)
            ? keywords
            : Enumerable.Empty<string>();
    }

    /// <summary>
    /// Get confidence score for a category match (0.0 to 1.0)
    /// </summary>
    public float GetMatchConfidence(string merchantName, ReceiptCategory category)
    {
        var normalizedMerchant = merchantName.ToLowerInvariant();
        var keywords = GetKeywordsForCategory(category);

        var matchingKeywords = keywords.Count(k =>
            normalizedMerchant.Contains(k.ToLowerInvariant()));

        if (matchingKeywords == 0)
            return 0f;

        // Exact match = 1.0, partial match = 0.5-0.9
        var exactMatch = keywords.Any(k =>
            normalizedMerchant.Equals(k.ToLowerInvariant()));

        return exactMatch ? 1.0f : 0.7f;
    }
}