using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System;
using LMS.DAL.Data;
using LMS.DAL.Interfaces;
using LMS.DAL.Repositories;
using LMS.BLL.Interfaces;
using LMS.BLL.Services;
using LMS.PL.Middleware;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});


#region Database Context

builder.Services.AddDbContext<LMSDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

#endregion

#region Repository Registration

builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ILanguageRepository, LanguageRepository>();
builder.Services.AddScoped<ICourseSectionRepository, CourseSectionRepository>();
builder.Services.AddScoped<ILectureRepository, LectureRepository>();
builder.Services.AddScoped<ILectureProgressRepository, LectureProgressRepository>();
builder.Services.AddScoped<IQuizRepository, QuizRepository>();
builder.Services.AddScoped<IQuizQuestionRepository, QuizQuestionRepository>();
builder.Services.AddScoped<IQuizOptionRepository, QuizOptionRepository>();
builder.Services.AddScoped<ICourseReviewRepository, CourseReviewRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<ICertificateRepository, CertificateRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

#endregion



#region App build
var app = builder.Build();
#endregion



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
