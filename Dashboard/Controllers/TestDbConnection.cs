using Microsoft.AspNetCore.Mvc;
using UsersMgmt.Data;


[ApiController]
[Route("api/testdbconnection")]
public class TestDbConnection : ControllerBase
{
    private readonly ApplicationDbContext? _context;
    public TestDbConnection(ApplicationDbContext? context)
    {
        _context = context;
    }

    [HttpGet]
    public ActionResult Get()
    {
        try
        {
            if (_context == null)
            {
                return StatusCode(500, "Database context is not available.");
            }
            return Ok("Database connection is successful.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Database connection failed: {ex.Message}");
        }
    }
}
