using Core.Entities;

namespace MVC.Models;
public class PostsListViewModel
{
    public List<Post> Posts { get; set; } = new();
    public Dictionary<Guid, string> TopicNames { get; set; } = new();
    

    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string SortBy { get; set; } = "ratinga";
    
 
    public int PageNumber { get; set; }  
    public Guid? TopicId { get; set; }   
}