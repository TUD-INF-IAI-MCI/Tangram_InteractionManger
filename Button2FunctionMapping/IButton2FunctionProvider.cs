namespace tud.mci.tangram.TangramLector.Button2FunctionMapping
{
    /// <summary>
    /// Offers functions to provide a device-related button/key-combination to function-name mappings definition.
    /// </summary>
    interface IButton2FunctionProvider
    {
        /// <summary>
        /// Gets a value indicating whether definitions should be combined or completely replaced.
        /// </summary>
        /// <value><c>true</c> if the definitions will be combined (SHOULD BE STANDARD); 
        /// otherwise, <c>false</c> if want to replace the definition completely.</value>
        bool CombineDefinitions { get; }

        /// <summary>
        /// Gets the button/key-combination to function-name mappings XML content string.
        /// The XML must follow the defined Schema ('FunctionMappingXSD.xsd').
        /// </summary>
        /// <returns>System.String.</returns>
        string GetMappingsXML();

    }
}
