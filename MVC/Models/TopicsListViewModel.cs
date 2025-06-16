using Core.Entities;
using Infrastructure.Repositories;

namespace MVC.Models;

public class TopicsListViewModel
{
    public List<TopicViewModel> Topics { get; set; } = new();
    public IEnumerable<Category> Categories { get; set; } = new List<Category>();
}

public class TopicViewModel
{
    public Topic Topic { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public bool IsUserSubscribed { get; set; }
}