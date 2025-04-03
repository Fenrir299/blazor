// Services/ErrorHandling/ErrorHandlingService.cs
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FFB.ContentTransformation.Services.ErrorHandling
{
    /// <summary>
    /// Service to handle errors in a consistent way across the application
    /// </summary>
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;

        public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
        {
            _logger = logger;
        }

        public async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action, string operationName)
        {
            try
            {
                var result = await action();
                return Result<T>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {OperationName} operation", operationName);

                var userFriendlyMessage = GetUserFriendlyMessage(ex);
                return Result<T>.Failure(userFriendlyMessage);
            }
        }

        private string GetUserFriendlyMessage(Exception ex)
        {
            // If we have a specific user-friendly message in the exception, use it
            if (ex.Message.StartsWith("Erreur lors de") || ex.Message.StartsWith("Limite de requêtes") ||
                ex.Message.StartsWith("Quota") || ex.Message.StartsWith("Le contenu a été filtré"))
            {
                return ex.Message;
            }

            // For specific exception types, provide more user-friendly messages
            return ex switch
            {
                TimeoutException => "L'opération a pris trop de temps. Veuillez réessayer plus tard.",
                ArgumentException => "Paramètres invalides pour l'opération.",
                UnauthorizedAccessException => "Vous n'avez pas les autorisations nécessaires pour cette opération.",
                _ => "Une erreur inattendue s'est produite. Veuillez réessayer ou contacter l'administrateur."
            };
        }
    }

    /// <summary>
    /// Interface for error handling service
    /// </summary>
    public interface IErrorHandlingService
    {
        Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action, string operationName);
    }

    /// <summary>
    /// Result class to handle operation results with potential errors
    /// </summary>
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Data { get; }
        public string? ErrorMessage { get; }

        private Result(bool isSuccess, T? data, string? errorMessage)
        {
            IsSuccess = isSuccess;
            Data = data;
            ErrorMessage = errorMessage;
        }

        public static Result<T> Success(T data)
        {
            return new Result<T>(true, data, null);
        }

        public static Result<T> Failure(string errorMessage)
        {
            return new Result<T>(false, default, errorMessage);
        }
    }
}