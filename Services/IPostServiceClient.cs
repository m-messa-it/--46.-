using System.Threading.Tasks;
using PostServiceFormsApp.Models;

namespace PostServiceFormsApp.Services
{
    public interface IPostServiceClient
    {
        // GET /postings
        Task<Posting[]> GetPostingsAsync();

        // GET /postings/{id}
        Task<Posting> GetPostingByIdAsync(int id);

        // POST /postings
        Task<Posting> CreatePostingAsync(Posting posting);

        // PUT /postings/{id}
        Task<Posting> UpdatePostingAsync(int id, Posting posting);

        // DELETE /postings/{id}
        Task DeletePostingAsync(int id);
    }
}
