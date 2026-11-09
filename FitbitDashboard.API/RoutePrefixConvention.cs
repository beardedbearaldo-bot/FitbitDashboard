using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

public class RoutePrefixConvention : IControllerModelConvention
{
    private readonly AttributeRouteModel _routePrefix;

    public RoutePrefixConvention(string prefix)
    {
        _routePrefix = new AttributeRouteModel(new RouteAttribute(prefix));
    }

    public void Apply(ControllerModel controller)
    {
        foreach (var selector in controller.Selectors)
        {
            if (selector.AttributeRouteModel != null)
            {
                // Combines your "api" prefix with the existing [Route] on the controller
                selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(_routePrefix, selector.AttributeRouteModel);
            }
            else
            {
                selector.AttributeRouteModel = _routePrefix;
            }
        }
    }
}
