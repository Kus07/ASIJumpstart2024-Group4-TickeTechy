using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IAgentService
    {
        int GetUserWithSmallestLoad(int departmentId);
    }
}
