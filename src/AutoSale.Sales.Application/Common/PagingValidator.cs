using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Common;

public static class PagingValidator
{
    public static Result Validate(int page, int pageSize)
    {
        if (page < 1)
        {
            return Result.Failure(ApplicationErrors.InvalidPage);
        }

        return pageSize is < 1 or > 100
            ? Result.Failure(ApplicationErrors.InvalidPageSize)
            : Result.Success();
    }
}
