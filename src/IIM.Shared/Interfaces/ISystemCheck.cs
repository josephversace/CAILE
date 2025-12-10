using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces;


public interface ISystemCheckService
{
	Task<SystemCheckResult> RunAsync();
}
