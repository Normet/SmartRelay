using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Normet.Cloud.Relay
{
    public class CustomerAsset
    {
        public string Customer { get; set; }
        public string ItemCode { get; set; }
        public string ItemRev { get; set; }
        public string DocId { get; set; }
        public string Model { get; set; }
        public string CustomerSerialNumber { get; set; }
        public string SerialNumber { get; set; }
        public string RealEngineHours { get; set; }
        public string CurrentCustomer { get; set; }
        public string Site { get; set; }
        public string StartupDate { get; set; }
        public List<AssetModule> Modules { get; set; }
    }

    public class AssetModule
    {
        public string Name
        {
            get
            {
                return $"{ItemNumber}-{Description}";
            }
        }
        public string ItemNumber { get; set; }
        public string Description { get; set; }
        public List<IncidentType> Incidents { get; set; }
        public void AddServiceTaskRange(IEnumerable<ServiceTask> range)
        {
            foreach(var item in range)
            {
                AddServiceTask(item);
            }
        }
        public void AddServiceTask(ServiceTask serviceTask)
        {
            var code = $"{ItemNumber}-{serviceTask.ServiceType}";
            IncidentType incident;
            if (Incidents == null) Incidents = new List<IncidentType>();

            if (!Incidents.Exists(i => i.Id == code))
            {
                incident = new IncidentType()
                {
                    Id = code,
                    Name = code
                };
                Incidents.Add(incident);
            }
            else
            {
                incident = Incidents.First(i => i.Id == code);
            }
            incident.ServiceTasks.Add(serviceTask);
        }
    }

    public class IncidentType
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<ServiceTask> ServiceTasks = new List<ServiceTask>();
    }

    public class ServiceTask
    {
        public string Id { get; set; }
        public string Rev { get; set; }
        public string DocDescr { get; set; }
        public string HasChildren { get; set; }
        public string ServiceType { get; set; }
        public string ServiceCategory { get; set; }
        public string ServiceSequence { get; set; }
        public string ServiceDuration { get; set; }
        public List<ServiceItem> Items { get; set; }
    }

    public class ServiceItem
    {
        public string ItemNumber { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public string Qty { get; set; }
    }
}
