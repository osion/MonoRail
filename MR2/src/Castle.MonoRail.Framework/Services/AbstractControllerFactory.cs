// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.MonoRail.Framework.Services
{
	using System;
	using Core;
	using Core.Logging;
	using Framework;

	/// <summary>
	/// Base implementation of <see cref="IControllerFactory"/>
	/// using the <see cref="DefaultControllerTree"/> to build an hierarchy
	/// of controllers and the areas they belong to.
	/// <seealso cref="DefaultControllerTree"/>
	/// </summary>
	public abstract class AbstractControllerFactory : IServiceEnabledComponent, IControllerFactory
	{
		/// <summary>
		/// The controller tree. A binary tree that contains
		/// all controllers registered
		/// </summary>
		private IControllerTree tree;
		
		/// <summary>
		/// The logger instance
		/// </summary>
		private ILogger logger = NullLogger.Instance;

		/// <summary>
		/// The logger factory for controller logger initialization
		/// </summary>
		private ILoggerFactory loggerFactory;

		/// <summary>
		/// Initializes an <c>AbstractControllerFactory</c> instance
		/// </summary>
		protected AbstractControllerFactory()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AbstractControllerFactory"/> class.
		/// </summary>
		/// <param name="tree">The tree.</param>
		protected AbstractControllerFactory(IControllerTree tree)
		{
			this.tree = tree;
		}
		
		/// <summary>
		/// Invoked by the framework in order to initialize the state
		/// </summary>
		public virtual void Initialize()
		{
			AddBuiltInControllers();
		}
		
		#region IServiceEnabledComponent implementation
		
		/// <summary>
		/// Invoked by the framework in order to give a chance to
		/// obtain other services
		/// </summary>
		/// <param name="provider">The service proviver</param>
		public virtual void Service(IServiceProvider provider)
		{
			loggerFactory = (ILoggerFactory) provider.GetService(typeof(ILoggerFactory));
			
			if (loggerFactory != null)
			{
				logger = loggerFactory.Create(typeof(AbstractControllerFactory));
			}

			tree = (IControllerTree) provider.GetService(typeof(IControllerTree));

			Initialize();
		}
		
		#endregion

		/// <summary>
		/// Implementors should perform their logic to
		/// return a instance of <see cref="IController"/>.
		/// If the <see cref="IController"/> can not be found,
		/// it should return <c>null</c>.
		/// </summary>
		/// <returns></returns>
		public virtual IController CreateController(string area, string controller)
		{
			area = area ?? String.Empty;
			
			return CreateControllerInstance(area, controller);
		}

		/// <summary>
		/// Creates the controller.
		/// </summary>
		/// <param name="controllerType">Type of the controller.</param>
		/// <returns></returns>
		public IController CreateController(Type controllerType)
		{
			try
			{
				var controller = (IController) Activator.CreateInstance(controllerType);
				var builtinController = controller as Controller;
				if (builtinController != null && loggerFactory != null)
				{
					builtinController.Logger = loggerFactory.Create(builtinController.GetType());
				}
				return controller;
			}
			catch (Exception ex)
			{
				logger.Error("Could not create controller instance. Activation failed.", ex);

				throw;
			}
		}

		/// <summary>
		/// Implementors should perform their logic
		/// to release the <see cref="IController"/> instance
		/// and its resources.
		/// </summary>
		/// <param name="controller"></param>
		public virtual void Release(IController controller)
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug("Controller released: " + controller.GetType());
			}

			controller.Dispose();
		}

		/// <summary>
		/// Gets the tree.
		/// </summary>
		/// <value>The tree.</value>
		public IControllerTree Tree
		{
			get { return tree; }
			set { tree = value; }
		}

		/// <summary>
		/// Register built-in controller that serve static files
		/// </summary>
		protected void AddBuiltInControllers()
		{
			if (logger.IsDebugEnabled)
			{
				logger.Debug("Registering built-in controllers");
			}
			
//			Tree.AddController("MonoRail", "Files", typeof(FilesController));
		}

		/// <summary>
		/// Creates the controller instance.
		/// </summary>
		/// <param name="area">The area.</param>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		protected virtual IController CreateControllerInstance(String area, String name)
		{
			if (logger.IsDebugEnabled)
			{
				logger.DebugFormat("Creating controller instance. Area '{0}' Name '{1}'", area, name);
			}

			var type = Tree.GetController(area, name);

			if (type == null)
			{
				logger.ErrorFormat("Controller not found. Area '{0}' Name '{1}'", area, name);
				
				throw new ControllerNotFoundException(area, name);
			}

			return CreateController(type);
		}
	}
}
