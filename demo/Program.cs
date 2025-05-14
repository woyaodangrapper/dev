// See https://aka.ms/new-console-template for more information

using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicaitonHostedServices();