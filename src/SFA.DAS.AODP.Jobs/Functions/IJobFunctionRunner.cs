namespace SFA.DAS.AODP.Jobs.Functions;

/// <summary>
/// Defines a generic approach to running job import functions.
/// </summary>
public interface IJobFunctionRunner
{
    /// <summary>
    /// Runs the job import function handling logging and job configuration.
    /// </summary>
    /// <param name="functionName">The name of the function to run.</param>
    /// <param name="username">The username running the import.</param>
    /// <param name="jobName">The name of the job to run, used to also load and update the job configuration.</param>
    /// <param name="doImport">The delegate that will run that performs the actual import for each different import process.</param>
    /// <param name="cancellationToken">Propagates a notification that the operation should be cancelled.</param>
    /// <returns>The ASP.NET Core result.</returns>
    Task<IActionResult> RunAsync(
        string functionName,
        string username,
        JobNames jobName,
        Func<JobControl, CancellationToken, Task<int>> doImport,
        CancellationToken cancellationToken);
}