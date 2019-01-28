﻿using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Exceptions;
using WebVella.Erp.Web.Models;
using WebVella.Erp.Web.Services;

namespace WebVella.Erp.Web.Components
{
	[PageComponent(Label = "Field Multiselect", Library = "WebVella", Description = "Multiple values can be selected from a provided list", Version = "0.0.1", IconClass = "fas fa-check-double")]
	public class PcFieldMultiSelect : PcFieldBase
	{
		protected ErpRequestContext ErpRequestContext { get; set; }

		public PcFieldMultiSelect([FromServices]ErpRequestContext coreReqCtx)
		{
			ErpRequestContext = coreReqCtx;
		}

		public class PcFieldMultiSelectOptions : PcFieldBaseOptions
		{
			[JsonProperty(PropertyName = "options")]
			public string Options { get; set; } = "";

			public static PcFieldMultiSelectOptions CopyFromBaseOptions(PcFieldBaseOptions input)
			{
				return new PcFieldMultiSelectOptions
				{
					LabelMode = input.LabelMode,
					LabelText = input.LabelText,
					Mode = input.Mode,
					Name = input.Name,
					Options = ""
				};
			}
		}

		public async Task<IViewComponentResult> InvokeAsync(PageComponentContext context)
		{
			ErpPage currentPage = null;
			try
			{
				#region << Init >>
				if (context.Node == null)
				{
					return await Task.FromResult<IViewComponentResult>(Content("Error: The node Id is required to be set as query param 'nid', when requesting this component"));
				}

				var pageFromModel = context.DataModel.GetProperty("Page");
				if (pageFromModel == null)
				{
					return await Task.FromResult<IViewComponentResult>(Content("Error: PageModel cannot be null"));
				}
				else if (pageFromModel is ErpPage)
				{
					currentPage = (ErpPage)pageFromModel;
				}
				else
				{
					return await Task.FromResult<IViewComponentResult>(Content("Error: PageModel does not have Page property or it is not from ErpPage Type"));
				}

				var baseOptions = InitPcFieldBaseOptions(context);
				var instanceOptions = PcFieldMultiSelectOptions.CopyFromBaseOptions(baseOptions);
				if (context.Options != null)
				{
					instanceOptions = JsonConvert.DeserializeObject<PcFieldMultiSelectOptions>(context.Options.ToString());
					////Check for connection to entity field
					//if (instanceOptions.TryConnectToEntity)
					//{
					//	var entity = context.DataModel.GetProperty("Entity");
					//	if (entity != null && entity is Entity)
					//	{
					//		var fieldName = instanceOptions.Name;
					//		var entityField = ((Entity)entity).Fields.FirstOrDefault(x => x.Name == fieldName);
					//		if (entityField != null && entityField is MultiSelectField)
					//		{
					//			var castedEntityField = ((MultiSelectField)entityField);
					//			//No options connected
					//		}
					//	}
					//}
				}
				var modelFieldLabel = "";
				var model = (PcFieldMultiSelectModel)InitPcFieldBaseModel(context, instanceOptions, label: out modelFieldLabel, targetModel: "PcFieldMultiSelectModel");
				if (String.IsNullOrWhiteSpace(instanceOptions.LabelText))
				{
					instanceOptions.LabelText = modelFieldLabel;
				}
				//PcFieldMultiSelectModel model = PcFieldMultiSelectModel.CopyFromBaseModel(baseModel);

				//Implementing Inherit label mode
				ViewBag.LabelMode = instanceOptions.LabelMode;
				ViewBag.Mode = instanceOptions.Mode;

				if (instanceOptions.LabelMode == LabelRenderMode.Undefined && baseOptions.LabelMode != LabelRenderMode.Undefined)
					ViewBag.LabelMode = baseOptions.LabelMode;

				if (instanceOptions.Mode == FieldRenderMode.Undefined && baseOptions.Mode != FieldRenderMode.Undefined)
					ViewBag.Mode = baseOptions.Mode;


				var componentMeta = new PageComponentLibraryService().GetComponentMeta(context.Node.ComponentName);
				#endregion


				#region << Init DataSources >>
				if (context.Mode != ComponentMode.Options && context.Mode != ComponentMode.Help)
				{
					dynamic valueResult = context.DataModel.GetPropertyValueByDataSource(instanceOptions.Value);
					if (valueResult == null)
					{
						model.Value = new List<string>();
					}
					else if (valueResult is List<string>)
					{
						model.Value = (List<string>)valueResult;
					}
					else if (valueResult is string)
					{
						var stringProcessed = false;
						if (String.IsNullOrWhiteSpace(valueResult))
						{
							model.Value = new List<string>();
							stringProcessed = true;
						}
						if (!stringProcessed && (((string)valueResult).StartsWith("{") || ((string)valueResult).StartsWith("[")))
						{
							try
							{
								model.Value = JsonConvert.DeserializeObject<List<string>>(valueResult.ToString());
								stringProcessed = true;
							}
							catch
							{
								stringProcessed = false;
								ViewBag.ExceptionMessage = "Value Json Deserialization failed!";
								ViewBag.Errors = new List<ValidationError>();
								return await Task.FromResult<IViewComponentResult>(View("Error"));
							}
						}
						if (!stringProcessed && ((string)valueResult).Contains(",") && !((string)valueResult).Contains("{") && !((string)valueResult).Contains("["))
						{
							var valueArray = ((string)valueResult).Split(',');
							model.Value = new List<string>(valueArray);
						}
					}
					else if (valueResult is List<Guid>)
					{
						model.Value = ((List<Guid>)valueResult).Select(x => x.ToString()).ToList();
					}
					else if (valueResult is Guid)
					{
						model.Value = new List<string>(valueResult.ToString());
					}


					var dataSourceOptions = new List<SelectOption>();
					dynamic optionsResult = context.DataModel.GetPropertyValueByDataSource(instanceOptions.Options);
					if (optionsResult == null) { }
					if (optionsResult is List<SelectOption>)
					{
						dataSourceOptions = (List<SelectOption>)optionsResult;
					}
					else if (optionsResult is string)
					{
						var stringProcessed = false;
						if (String.IsNullOrWhiteSpace(optionsResult))
						{
							dataSourceOptions = new List<SelectOption>();
							stringProcessed = true;
						}
						if (!stringProcessed && (((string)optionsResult).StartsWith("{") || ((string)optionsResult).StartsWith("[")))
						{
							try
							{
								dataSourceOptions = JsonConvert.DeserializeObject<List<SelectOption>>(optionsResult);
								stringProcessed = true;
							}
							catch
							{
								stringProcessed = false;
								ViewBag.ExceptionMessage = "Options Json Deserialization failed!";
								ViewBag.Errors = new List<ValidationError>();
								return await Task.FromResult<IViewComponentResult>(View("Error"));
							}
						}
						if (!stringProcessed && ((string)optionsResult).Contains(",") && !((string)optionsResult).Contains("{") && !((string)optionsResult).Contains("["))
						{
							var optionsArray = ((string)optionsResult).Split(',');
							var optionsList = new List<SelectOption>();
							foreach (var optionString in optionsArray)
							{
								optionsList.Add(new SelectOption(optionString, optionString));
							}
							dataSourceOptions = optionsList;
						}
					}
					if (dataSourceOptions.Count > 0)
					{
						model.Options = dataSourceOptions;
					}
				}

				#endregion


				ViewBag.Options = instanceOptions;
				ViewBag.Model = model;
				ViewBag.Node = context.Node;
				ViewBag.ComponentMeta = componentMeta;
				ViewBag.RequestContext = ErpRequestContext;
				ViewBag.AppContext = ErpAppContext.Current;

				switch (context.Mode)
				{
					case ComponentMode.Display:
						return await Task.FromResult<IViewComponentResult>(View("Display"));
					case ComponentMode.Design:
						return await Task.FromResult<IViewComponentResult>(View("Design"));
					case ComponentMode.Options:
						return await Task.FromResult<IViewComponentResult>(View("Options"));
					case ComponentMode.Help:
						return await Task.FromResult<IViewComponentResult>(View("Help"));
					default:
						ViewBag.ExceptionMessage = "Unknown component mode";
						ViewBag.Errors = new List<ValidationError>();
						return await Task.FromResult<IViewComponentResult>(View("Error"));
				}
			}
			catch (ValidationException ex)
			{
				ViewBag.ExceptionMessage = ex.Message;
				ViewBag.Errors = new List<ValidationError>();
				return await Task.FromResult<IViewComponentResult>(View("Error"));
			}
			catch (Exception ex)
			{
				ViewBag.ExceptionMessage = ex.Message;
				ViewBag.Errors = new List<ValidationError>();
				return await Task.FromResult<IViewComponentResult>(View("Error"));
			}
		}
	}
}