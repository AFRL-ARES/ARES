using RestDevice.Commands.Responses.JsonStructures;
using RestDevice.Generics;

namespace RestDevice.Structure;

public static class JsonConversionHelper
{

  public static List<RestDeviceVariable> ConvertFromJsonVariables(this List<Variable> jsonVariables)
  {
    var internalVariables = new List<RestDeviceVariable>();

    foreach(var jsonVariable in jsonVariables)
    {
      var internalVar = new RestDeviceVariable(jsonVariable.Name, jsonVariable.Description, jsonVariable.Path, jsonVariable.DataType);
      internalVar.Unit = jsonVariable.Unit;
      internalVar.Readable = jsonVariable.Readable;
      internalVar.Writable = jsonVariable.Writable;
      internalVar.Uncertainty = jsonVariable.Uncertainty;

      internalVariables.Add(internalVar);
    }

    return internalVariables;
  }

  public static List<RestDeviceMethod> ConvertFromJsonMethods(this List<Function> jsonMethods)
  {
    var internalMethods = new List<RestDeviceMethod>();

    foreach(var jsonMethod in jsonMethods)
    {
      var internalMethod = new RestDeviceMethod();
      internalMethod.Name = jsonMethod.Name;
      internalMethod.Description = jsonMethod.Description;
      internalMethod.Path = jsonMethod.Path;

      foreach(var jsonParam in jsonMethod.Parameters)
      {
        var internalParam = new RestDeviceParameter(jsonParam.Name, jsonParam.DataType);
        internalParam.Minimum = jsonParam.Minimum;
        internalParam.Maximum = jsonParam.Maximum;
        internalParam.Unit = jsonParam.unit;

        internalMethod.Parameters.Add(internalParam);
      }

      foreach(var jsonReturnParam in jsonMethod.Returns)
      {
        var internalReturnParam = new RestDeviceOutput(jsonReturnParam.Name, jsonReturnParam.DataType, jsonReturnParam.Description);
        internalReturnParam.Unit = jsonReturnParam.Unit;
        internalMethod.Output.Add(internalReturnParam);
      }

      internalMethods.Add(internalMethod);
    }

    return internalMethods;
  }

  public static Type DetermineType(string stringType)
  {
    switch(stringType.ToLowerInvariant())
    {
      case ("string"):
        return typeof(string);

      case ("int"):
      case ("integer"):
        return typeof(int);

      case ("float"):
        return typeof(float);

      case ("double"):
        return typeof(double);

      case ("boolean"):
      case ("bool"):
        return typeof(bool);
    }

    //TODO: Error handling? Maybe we refuse to use the method if this can't be discovered properly?
    return typeof(string);
  }
}
