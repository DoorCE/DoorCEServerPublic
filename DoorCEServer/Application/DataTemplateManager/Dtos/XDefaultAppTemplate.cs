using DoorCEModel.Infrastructure.DataModel.Applications;

namespace DoorCEServer.Application.DataTemplateManager.Dtos;

public class XDefaultAppTemplate
{
	public Dictionary<string, string> UseCaseScenarios { get; set; } = new();
	public ICollection<XConcept> AuxiliaryConcepts { get; set; } = [];
}

public static class XDefaultAppTemplateFactory
{
	public static XDefaultAppTemplate Get(string conceptName, string language, bool includeSubmit = true)
	{
        string conceptNameLower = conceptName.ToLower();
		XDefaultAppTemplate template = new XDefaultAppTemplate
        {
            UseCaseScenarios = new Dictionary<string, string>
            {
                {
                    "Start", "Main scenario\n" +
                             "00: User <select> application\n" +
                             "01: System <show> main menu\n" +
                             "02: User <invoke> Show " + conceptNameLower + " list, Add " + conceptNameLower 
                             + (includeSubmit ? ", Submit data items" : "") +
                             "\n-> rejoin 01\n"
                },
                {
                    "Show " + conceptNameLower + " list", "Main scenario\n" +
                                                          "00: User <select> Show " + conceptNameLower + " list\n" +
                                                          "01: System <read> " + conceptNameLower + " list\n" +
                                                          "02: System <show> " + conceptNameLower + " list window\n" +
                                                          "03: User <select> close\n" +
                                                          "-> end ! OK\n" +
                                                          "Scenario\n" +
                                                          "02: -\"-\n" +
                                                          "A1: User <enter> " + conceptNameLower + " list\n" +
                                                          "A2: User <invoke> Edit " + conceptNameLower + ", Delete " +
                                                          conceptNameLower + "\n" +
                                                          "-> rejoin 01\n" +
                                                          "Scenario\n" +
                                                          "02: -\"-\n" +
                                                          "B1: User <invoke> Add " + conceptNameLower + "\n" +
                                                          "-> rejoin 01\n"
                },
                {
                    "Add " + conceptNameLower, "Main scenario\n" +
                                "00: User <select> Add " + conceptNameLower + "\n" +
                                "01: System <show> add " + conceptNameLower + " form\n" +
                                "02: User <enter> " + conceptNameLower + "\n" +
                                "03: User <select> submit\n" +
                                "04: System <check> " + conceptNameLower + "\n" +
                                "[" + conceptNameLower + " ? OK]\n" +
                                "05: System <update> " + conceptNameLower + "\n" +
                                "06: System <show> " + conceptNameLower + " added\n" +
                                "07: User <select> close\n" + 
                                "-> end ! OK\n" +
                                "Scenario\n" +
                                "04: -\"-\n" +
                                "[" + conceptNameLower + " ? NOTOK]\n" +
                                "A1: System <show> error " + conceptNameLower + " not added\n" +
                                "A2: User <select> close\n" + 
                                "-> end ! NOTOK\n" +
                                "Scenario\n" +
                                "01: -\"-\n" +
                                "B1: User <select> cancel\n" +
                                "-> end ! CANCEL\n"
                },
                {
                    "Edit " + conceptNameLower, "Main scenario\n" +
                                                "{" + conceptNameLower + "}\n" + 
                                                "00: User <select> Edit " + conceptNameLower + "\n" + 
                                                "01: System <read> " + conceptNameLower + "\n" +
                                                "02: System <show> edit " + conceptNameLower + " form\n" +
                                                "03: User <enter> " + conceptNameLower + "\n" +
                                                "04: User <select> submit\n" +
                                                "05: System <check> " + conceptNameLower + "\n" +
                                                "[" + conceptNameLower + " ? OK]\n" +
                                                "06: System <update> " + conceptNameLower + "\n" +
                                                "07: System <show> " + conceptNameLower + " updated\n" +
                                                "08: User <select> close\n" + 
                                                "-> end ! OK\n" +
                                                "Scenario\n" +
                                                "05: -\"-\n" +
                                                "[" + conceptNameLower + " ? NOTOK]\n" +
                                                "A1: System <show> error " + conceptNameLower + " not updated\n" +
                                                "A2: User <select> close\n" + 
                                                "-> end ! NOTOK\n" +
                                                "Scenario\n" +
                                                "02: -\"-\n" +
                                                "B1: User <select> cancel\n" +
                                                "-> end ! CANCEL\n"
                },
                {
                    "Delete " + conceptNameLower, "Main scenario\n" +
                                                  "{" + conceptNameLower + "}\n" +
                                                  "00: User <select> Delete " + conceptNameLower + "\n" +
                                                  "01: System <show> Please confirm deletion\n" +
                                                  "02: User <select> confirm\n" +
                                                  "03: System <delete> " + conceptNameLower + "\n" +
                                                  "04: System <show> " + conceptNameLower + " deleted\n" +
                                                  "05: User <select> close\n" +
                                                  "-> end ! OK\n" +
                                                  "Scenario\n" +
                                                  "01: -\"-\n" +
                                                  "A1: User <select> cancel\n" +
                                                  "-> end ! CANCEL\n"
                }
            },
            AuxiliaryConcepts =
            [
                new XConcept()
                {
                    Name = conceptName + " List", Properties = new Dictionary<string, XPropertyValue>
                    {
                        {
                            conceptNameLower + "s", new XPropertyValue
                            {
                                Type = "array",
                                Items = new XPropertyValue { Type = "reference", Target = conceptName }
                            }
                        }
                    }
                }
            ]
        };

        if (includeSubmit)
            template.UseCaseScenarios.Add("Submit data items", "Main scenario\n" +
                                                               "00: User <select> Submit data items\n" +
                                                               "01: System <show> Please confirm submission\n" +
                                                               "02: User <select> confirm\n" +
                                                               "03: System <execute> submit\n" +
                                                               "04: System <show> data items submitted\n" +
                                                               "05: User <select> close\n" +
                                                               "-> end ! OK\n" +
                                                               "Scenario\n" +
                                                               "01: -\"-\n" +
                                                               "A1: User <select> cancel\n" +
                                                               "-> end ! CANCEL\n");
        return template;
    }
}