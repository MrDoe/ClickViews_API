using Microsoft.AspNetCore.Mvc;
using ClickSphere_API.Services;
namespace ClickSphere_API.Controllers;

/// <summary>
/// The base class for ODBC connections from ClickHouse to Microsoft SQL Server.
/// </summary>
[ApiController]
public class OdbcController(IApiViewService viewService) : ControllerBase
{
    private readonly IApiViewService _viewService = viewService;

    /// <summary>
    /// Get columns from a view in the ODBC connection.
    /// </summary>
    /// <param name="view">The view to get.</param>
    /// <param name="column">The column to get.</param>
    /// <returns>The list of views.</returns>
    [HttpGet]
    [Route("/odbc/columns")]
    public async Task<IList<string>> GetColumnsFromODBC(string view, string column)
    {
        if(string.IsNullOrEmpty(view) || string.IsNullOrEmpty(column))
        {
            return [];
        }
        return await _viewService.GetColumnsFromODBC(view, column);
    }

    /// <summary>
    /// Get a SQL Server view definition from the ODBC connection.
    /// </summary>
    /// <param name="view">The view to import from the ODBC database.</param>
    /// <param name="dropExisting">Whether to drop the existing view.</param>
    /// <returns>The view definition.</returns>
    [HttpGet]
    [Route("/odbc/importView")]
    public async Task<string> ImportViewFromODBC(string view, bool dropExisting = false)
    {
        if(string.IsNullOrEmpty(view))
        {
            return "Invalid request";
        }
        return await _viewService.ImportViewFromODBC(view, dropExisting);
    }

    /// <summary>
    /// Get views from the ODBC connection.
    /// </summary>
    /// <returns>The list of views.</returns>
    [HttpGet]
    [Route("/odbc/views")]
    public async Task<IList<string>> GetViewsFromODBC()
    {
        return await _viewService.GetViewsFromODBC();
    }
}