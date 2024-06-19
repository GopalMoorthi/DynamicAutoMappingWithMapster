using Mapster;
using System;
using System.ComponentModel;
using System.Reflection;

namespace MapsterWithDynamicMapping
{
    public static class Program
    {
        private static readonly Dictionary<string, Type> _employeeTypes = new Dictionary<string, Type>
        {
            {"Employee" , typeof(Employee) },
            {"Manager" , typeof(Manager) }
        };
        public static void Main(string[] args)
        {
            TypeAdapterConfig<ResourceModel, Employee>
                .NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.Role, src => src.Role);

            TypeAdapterConfig<ResourceModel, Manager>
                .NewConfig()
                .Map(dest => dest.Id, src => src.Id)
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.ManagerLevel, src => src.Level);

            var employee = new ResourceModel { 
                Id = 1,
                Name = "Test",
                Level = "C3",
                Role = "Employee",
                EmployeeType = "Employee"
            };

            var manager = new ResourceModel
            {
                Id = 1,
                Name = "Test",
                Level = "C3",
                Role = "Employee",
                EmployeeType = "Manager"
            };

            var employeeContent = new Dictionary<string, object>();
            var employeeResult = GetResourceModel(employee.EmployeeType, employee, ref employeeContent, false);

            var managerContent = new Dictionary<string, object>();
            var managerResult = GetResourceModel(manager.EmployeeType, manager, ref managerContent, false);

        }

        public static TModel GetModelContent<TRequest, TModel>(TRequest input, bool isPatch = false)
        {
            var result = input.Adapt<TModel>();
            return result;
        }

        public static bool GetResourceModel<TRequest>(string typeCode, TRequest request, ref Dictionary<string, object>? contents, bool isPatch = false)
        {
            try
            {
                _employeeTypes.TryGetValue(typeCode, out var modelType);

                if (modelType == null)
                    return default;

                var methodInfo = typeof(Program).GetMethod(nameof(GetModelContent));

                var genericMethodInfo = methodInfo?.MakeGenericMethod(typeof(TRequest), modelType);

                contents = genericMethodInfo?.Invoke(typeof(Program), new object[] { request, isPatch }).ConvertNonEmptyToDictionary();

                return true;
            }
            catch
            {
                return default;
            }
        }

        public static Dictionary<string, object> ConvertNonEmptyToDictionary<TSource>(this TSource source, bool isPatch = false)
        {
            if (source == null)
                return null;

            var type = source.GetType();
            var props = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public);


            var dict = new Dictionary<string, object>();

            foreach (var prop in props)
            {
                if (prop.Name.EndsWith("_Ignore"))
                    continue;

                var value = prop.GetValue(source, null);

                if (value != null && (isPatch || !string.IsNullOrWhiteSpace(value.ToString())))
                {
                    dict[prop.Name] = value;
                }
                else if (isPatch)
                {
                    var properties = prop.GetCustomAttributes();

                    if (properties?.Count() > 0)
                    {
                        if (properties.FirstOrDefault() is DescriptionAttribute property)
                        {
                            var isBlank = Convert.ToBoolean(type.GetProperty(property.Description)?.GetValue(source, null));

                            if (isBlank)
                                dict[prop.Name] = string.Empty;
                        }
                    }
                }
            }

            return dict;
        }
    }


    public class ResourceModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }

        public string Level { get; set; }
        public string EmployeeType { get; set; }
        
    }

    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
    }

    public class Manager
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ManagerLevel { get; set; }
    }
}