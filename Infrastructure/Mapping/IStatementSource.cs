using System.Collections.Generic;
using System.Data;

namespace Infrastructure.Mapping
{
    public interface IStatementSource
    {
        string Query { get; }
        List<IDbDataParameter> Parameters { get; }
    }
}
