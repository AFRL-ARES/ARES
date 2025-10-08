using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AresService.DbDesignFactories;
public class AresDbContextFactory : BaseDesignFactory<AresDbContext>
{
}