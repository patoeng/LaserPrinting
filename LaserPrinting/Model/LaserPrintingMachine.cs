using System;
using System.Collections.Generic;
using System.Linq;
using OpcenterWikLibrary;
using Camstar.WCF.ObjectStack;


namespace LaserPrinting.Model
{
    public class LaserPrintingMachine
    {
        public ResourceStatusDetails ResourceStatusDetails { get;  protected set; }
        public MfgOrderChanges ManufacturingChanges { get; protected set; }
        public ProductChanges ProductChanges { get; protected set; }
        public ServiceUtil ServiceUtil { get; protected set; } = new ServiceUtil();


        public void SetResourceStatusDetails(ResourceStatusDetails resourceStatusDetails)
        {
            ResourceStatusDetails = resourceStatusDetails;
        }
        public void SetManufacturingChanges(MfgOrderChanges manufacturingChanges)
        {
            ManufacturingChanges = manufacturingChanges;
        }
        public void SetProductChanges(ProductChanges productChanges)
        {
            ProductChanges = productChanges;
        }
      
    }
}
