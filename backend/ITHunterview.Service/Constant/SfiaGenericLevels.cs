using System.Collections.Generic;
using System.Text;

namespace ITHunterview.Service.Constant
{
    public class SfiaGenericLevelData
    {
        public int Level { get; set; }
        public string Essence { get; set; }
        public string GuidanceNotes { get; set; }
        public Dictionary<string, string> CoreAttributes { get; set; }
        public Dictionary<string, string> BusinessSkills { get; set; }
    }

    public static class SfiaGenericLevels
    {
        public static readonly Dictionary<string, (string Description, string ShortCode)> Attributes = new Dictionary<string, (string, string)>
        {
            { "Autonomy", ("The level of independence, discretion and accountability for results in your role.", "AUTO") },
            { "Influence", ("The reach and impact of your decisions and actions, both within and outside the organisation.", "INFL") },
            { "Complexity", ("The range and intricacy of tasks and responsibilities that come with your role.", "COMP") },
            { "Knowledge", ("The depth and breadth of understanding required to perform and influence work effectively.", "KNGE") },
            { "Collaboration", ("Working effectively with others, sharing resources and coordinating efforts to achieve shared objectives.", "COLL") },
            { "Communication", ("Exchanging information, ideas and insights clearly to enable mutual understanding and cooperation.", "COMM") },
            { "Improvement mindset", ("Continuously identifying opportunities to refine work practices, processes, products, or services for greater efficiency and impact.", "IMPM") },
            { "Creativity", ("Generating and applying innovative ideas to enhance processes, solve problems and drive organisational success.", "CRTY") },
            { "Decision-making", ("Applying critical thinking to evaluate options, assess risks and select the most appropriate course of action.", "DECM") },
            { "Digital mindset", ("Embracing and effectively using digital tools and technologies to enhance performance and productivity.", "DIGI") },
            { "Leadership", ("Guiding and influencing individuals or teams to align actions with strategic goals and drive positive outcomes.", "LEAD") },
            { "Learning and development", ("Continuously acquiring new knowledge and skills to enhance personal and organisational performance.", "LADV") },
            { "Planning", ("Taking a systematic approach to organising tasks, resources and timelines to meet defined goals.", "PLAN") },
            { "Problem-solving", ("Analysing challenges, applying logical methods and developing effective solutions to overcome obstacles.", "PROB") },
            { "Adaptability", ("Adjusting to change and persisting through challenges at personal, team and organisational levels.", "ADAP") },
            { "Security, privacy and ethics", ("Ensuring the protection of sensitive information, upholding privacy of data and individuals, and demonstrating ethical conduct within and outside the organisation.", "SCPE") }
        };

        public static readonly Dictionary<int, SfiaGenericLevelData> Matrix = new Dictionary<int, SfiaGenericLevelData>
        {
            {
                1, new SfiaGenericLevelData
                {
                    Level = 1,
                    Essence = @"Performs routine tasks under close supervision, follows instructions, and requires guidance to complete their work. Learns and applies basic skills and knowledge.",
                    GuidanceNotes = @"SFIA Levels represent levels of responsibility in the workplace. Each successive level describes increasing impact, responsibility and accountability.

Autonomy, influence and complexity are generic attributes that indicate the level of responsibility.
Business skills and behavioural factors describe the behaviours required to be effective at each level.
The knowledge attribute defines the depth and breadth of understanding required to perform and influence work effectively.
Understanding these attributes will help you get the most out of SFIA. They are critical to understanding and applying the levels described in the SFIA skill descriptions.",
                    CoreAttributes = new Dictionary<string, string>
                    {
                        { "Autonomy", @"Follows instructions and works under close direction. Receives specific instructions and guidance, has work closely reviewed." },
                        { "Influence", @"When required, contributes to team discussions with immediate colleagues." },
                        { "Complexity", @"Performs routine activities in a structured environment." },
                        { "Knowledge", @"Applies basic knowledge to perform routine, well-defined, predictable role-specific tasks." },
                    },
                    BusinessSkills = new Dictionary<string, string>
                    {
                        { "Decision-making", @"Uses little discretion in attending to enquiries.
Is expected to seek guidance in unexpected situations." },
                        { "Planning", @"Confirms required steps for individual tasks." },
                        { "Collaboration", @"Works mostly on their own tasks and interacts with their immediate team only. Develops an understanding of how their work supports others." },
                        { "Problem-solving", @"Works towards understanding the issue and seeks assistance in resolving unexpected problems." },
                        { "Improvement mindset", @"Identifies opportunities for improvement in own tasks. Suggests basic enhancements when prompted." },
                        { "Creativity", @"Participates in the generation of new ideas when prompted." },
                        { "Communication", @"Communicates with immediate team to understand and deliver on their assigned tasks. Observes, listens, and with encouragement, asks questions to seek information or clarify instructions." },
                        { "Leadership", @"Proactively increases their understanding of their work tasks and responsibilities." },
                        { "Adaptability", @"Accepts change and is open to new ways of working." },
                        { "Learning and development", @"Applies newly acquired knowledge to develop  skills for their role. Contributes to identifying own development opportunities." },
                        { "Digital mindset", @"Has basic digital skills to learn and use applications, processes and tools for their role." },
                        { "Security, privacy and ethics", @"Develops an understanding of the rules and expectations of their role and the organisation." },
                    }
                }
            },
            {
                2, new SfiaGenericLevelData
                {
                    Level = 2,
                    Essence = @"Provides assistance to others, works under routine supervision, and uses their discretion to address routine problems. Actively learns through training and on-the-job experiences.",
                    GuidanceNotes = @"SFIA Levels represent levels of responsibility in the workplace. Each successive level describes increasing impact, responsibility and accountability.

Autonomy, influence and complexity are generic attributes that indicate the level of responsibility.
Business skills and behavioural factors describe the behaviours required to be effective at each level.
The knowledge attribute defines the depth and breadth of understanding required to perform and influence work effectively.
Understanding these attributes will help you get the most out of SFIA. They are critical to understanding and applying the levels described in the SFIA skill descriptions.",
                    CoreAttributes = new Dictionary<string, string>
                    {
                        { "Autonomy", @"Works under routine direction. Receives instructions and guidance, has work regularly reviewed." },
                        { "Influence", @"Is expected to contribute to team discussions with immediate team members. Works alongside team members, contributing to team decisions. When the role requires, interacts with people outside their team, including internal colleagues and external contacts." },
                        { "Complexity", @"Performs a range of work activities in varied environments." },
                        { "Knowledge", @"Applies knowledge of common workplace tasks and practices to support team activities under guidance." },
                    },
                    BusinessSkills = new Dictionary<string, string>
                    {
                        { "Decision-making", @"Uses limited discretion in resolving issues or enquiries.
Decides when to seek guidance in unexpected situations." },
                        { "Planning", @"Plans own work within short time horizons in an organised way." },
                        { "Collaboration", @"Understands the need to collaborate with their team and considers user/customer needs." },
                        { "Problem-solving", @"Investigates and resolves routine issues." },
                        { "Improvement mindset", @"Proposes ideas to improve own work area.
Implements agreed changes to assigned work tasks." },
                        { "Creativity", @"Applies creative thinking to suggest new ways to approach a task and solve problems." },
                        { "Communication", @"Communicates familiar information with immediate team and stakeholders directly related to their role.
Listens to gain understanding and asks relevant questions to clarify or seek further information." },
                        { "Leadership", @"Takes ownership to develop their work experience." },
                        { "Adaptability", @"Adjusts to different team dynamics and work requirements.
Participates in team adaptation processes." },
                        { "Learning and development", @"Absorbs and applies new information to tasks.
Recognises personal skills and knowledge gaps and seeks learning opportunities to address them." },
                        { "Digital mindset", @"Has sufficient digital skills for their role; understands and uses appropriate methods, tools, applications and processes." },
                        { "Security, privacy and ethics", @"Has a good understanding of their role and the organisation’s rules and expectations." },
                    }
                }
            },
            {
                3, new SfiaGenericLevelData
                {
                    Level = 3,
                    Essence = @"Performs varied tasks, sometimes complex and non-routine, using standard methods and procedures. Works under general direction, exercises discretion, and manages own work within deadlines. Proactively enhances skills and impact in the workplace.",
                    GuidanceNotes = @"SFIA Levels represent levels of responsibility in the workplace. Each successive level describes increasing impact, responsibility and accountability.

Autonomy, influence and complexity are generic attributes that indicate the level of responsibility.
Business skills and behavioural factors describe the behaviours required to be effective at each level.
The knowledge attribute defines the depth and breadth of understanding required to perform and influence work effectively.
Understanding these attributes will help you get the most out of SFIA. They are critical to understanding and applying the levels described in the SFIA skill descriptions.",
                    CoreAttributes = new Dictionary<string, string>
                    {
                        { "Autonomy", @"Works under general direction to complete assigned tasks. Receives guidance and has work reviewed at agreed milestones. When required, delegates routine tasks to others within own team." },
                        { "Influence", @"Works with and influences team decisions. Has a transactional level of contact with people outside their team, including internal colleagues and external contacts." },
                        { "Complexity", @"Performs a range of work, sometimes complex and non-routine, in varied environments." },
                        { "Knowledge", @"Applies knowledge of a range of role-specific practices to complete tasks within defined boundaries and has an appreciation of how this knowledge applies to the wider business context." },
                    },
                    BusinessSkills = new Dictionary<string, string>
                    {
                        { "Decision-making", @"Uses discretion in identifying and responding to complex issues related to own assignments.
Determines when issues should be escalated to a higher level." },
                        { "Planning", @"Organises and keeps track of own work (and others where needed) to meet agreed timescales." },
                        { "Collaboration", @"Understands and collaborates on the analysis of user/customer needs and represents this in their work." },
                        { "Problem-solving", @"Applies a methodical approach to investigate and evaluate options to resolve routine and moderately complex issues." },
                        { "Improvement mindset", @"Identifies and implements improvements in own work area.
Contributes to team-level process enhancements." },
                        { "Creativity", @"Applies and contributes to creative thinking techniques to contribute new ideas for their own work and for team activities." },
                        { "Communication", @"Communicates with team and stakeholders inside and outside the organisation clearly explaining and presenting information.
Contributes to a range of work-related conversations and listens to others to gain an understanding and asks probing questions relevant to their role." },
                        { "Leadership", @"Provides basic guidance and support to less experienced team members as needed." },
                        { "Adaptability", @"Adapts and is responsive to change and shows initiative in adopting new methods or technologies." },
                        { "Learning and development", @"Absorbs and applies new information effectively with the ability to share learnings with colleagues.
Takes the initiative in identifying and negotiating their own appropriate development opportunities." },
                        { "Digital mindset", @"Explores and applies relevant digital tools and skills for their role.
Understands and effectively applies appropriate methods, tools, applications and processes." },
                        { "Security, privacy and ethics", @"Applies appropriate professionalism and working practices and knowledge to work." },
                    }
                }
            },
            {
                4, new SfiaGenericLevelData
                {
                    Level = 4,
                    Essence = @"Performs diverse complex activities, supports and guides others, delegates tasks when appropriate, works autonomously under general direction, and contributes expertise to deliver team objectives.",
                    GuidanceNotes = @"SFIA Levels represent levels of responsibility in the workplace. Each successive level describes increasing impact, responsibility and accountability.

Autonomy, influence and complexity are generic attributes that indicate the level of responsibility.
Business skills and behavioural factors describe the behaviours required to be effective at each level.
The knowledge attribute defines the depth and breadth of understanding required to perform and influence work effectively.
Understanding these attributes will help you get the most out of SFIA. They are critical to understanding and applying the levels described in the SFIA skill descriptions.",
                    CoreAttributes = new Dictionary<string, string>
                    {
                        { "Autonomy", @"Works under general direction within a clear framework of accountability. Exercises considerable personal responsibility and autonomy.
When required, plans, schedules, and delegates work to others, typically within own team." },
                        { "Influence", @"Influences projects and team objectives. Has a tactical level of contact with people outside their team, including internal colleagues and external contacts." },
                        { "Complexity", @"Work includes a broad range of complex technical or professional activities in varied contexts." },
                        { "Knowledge", @"Applies knowledge across different areas in their field, integrating this knowledge to perform complex and diverse tasks. Applies a working knowledge of the organisation’s domain." },
                    },
                    BusinessSkills = new Dictionary<string, string>
                    {
                        { "Decision-making", @"Uses judgment and substantial discretion in identifying and responding to complex issues and assignments related to projects and team objectives.
Escalates when scope is impacted." },
                        { "Planning", @"Plans, schedules and monitors work to meet given personal and/or team objectives and processes, demonstrating an analytical approach to meet time and quality targets." },
                        { "Collaboration", @"Facilitates collaboration between stakeholders who share common objectives.
Engages with and contributes to the work of cross-functional teams to ensure that user/customer needs are being met throughout the deliverable/scope of work." },
                        { "Problem-solving", @"Investigates the cause and impact, evaluates options and resolves a broad range of complex issues." },
                        { "Improvement mindset", @"Encourages and supports team discussions on improvement initiatives.
Implements procedural changes within a defined scope of work." },
                        { "Creativity", @"Applies, facilitates and develops creative thinking concepts and finds alternative ways to approach team outcomes." },
                        { "Communication", @"Communicates with both technical and non-technical audiences including team and stakeholders inside and outside the organisation.
As required, takes the lead in explaining complex concepts to support decision making.
Listens and asks insightful questions to identify different perspectives to clarify and confirm understanding." },
                        { "Leadership", @"Leads, supports or guides team members.
Develops solutions for complex work activities related to assignments.
Demonstrates an understanding of risk factors in their work.
Contributes specialist expertise to requirements definition in support of proposals." },
                        { "Adaptability", @"Enables others to adapt and change in response to challenges and changes in the work environment." },
                        { "Learning and development", @"Rapidly absorbs and critically assesses new information and applies it effectively.
Maintains an understanding of emerging practices and their application and takes responsibility for driving own and team members’ development opportunities." },
                        { "Digital mindset", @"Maximises the capabilities of applications for their role and evaluates and supports the use of new technologies and digital tools.
Selects appropriately from, and assesses the impact of change to applicable standards, methods, tools, applications and processes relevant to own specialism." },
                        { "Security, privacy and ethics", @"Adapts and applies applicable standards, recognising their importance in achieving team outcomes." },
                    }
                }
            },
            {
                5, new SfiaGenericLevelData
                {
                    Level = 5,
                    Essence = @"Provides authoritative guidance in their field and works under broad direction. Accountable for delivering significant work outcomes, from analysis through execution to evaluation.",
                    GuidanceNotes = @"SFIA Levels represent levels of responsibility in the workplace. Each successive level describes increasing impact, responsibility and accountability.

Autonomy, influence and complexity are generic attributes that indicate the level of responsibility.
Business skills and behavioural factors describe the behaviours required to be effective at each level.
The knowledge attribute defines the depth and breadth of understanding required to perform and influence work effectively.
Understanding these attributes will help you get the most out of SFIA. They are critical to understanding and applying the levels described in the SFIA skill descriptions.",
                    CoreAttributes = new Dictionary<string, string>
                    {
                        { "Autonomy", @"Works under broad direction. Work is self-initiated, consistent with agreed operational and budgetary requirements for meeting allocated technical and/or group objectives. Defines tasks and delegates work to teams and individuals within area of responsibility." },
                        { "Influence", @"Influences critical decisions in their domain.  Has operational level contact impacting execution and implementation with internal colleagues and external contacts. Has significant influence over the allocation and management of resources required to deliver projects." },
                        { "Complexity", @"Performs an extensive range of complex technical and/or professional work activities, requiring the application of fundamental principles in a range of unpredictable contexts." },
                        { "Knowledge", @"Applies knowledge to interpret complex situations and offer authoritative advice. Applies in-depth expertise in specific fields, with a broader understanding across industry/business." },
                    },
                    BusinessSkills = new Dictionary<string, string>
                    {
                        { "Decision-making", @"Uses judgement to make informed decisions on actions to achieve organisational outcomes such as meeting targets, deadlines, and budget.
Raises issues when objectives are at risk." },
                        { "Planning", @"Analyses, designs, plans, establishes milestones, and executes and evaluates work to time, cost and quality targets." },
                        { "Collaboration", @"Facilitates collaboration between stakeholders who have diverse objectives.
Ensures collaborative ways of working throughout all stages of work to meet user/customer needs.
Builds effective relationships across the organisation and with customers, suppliers and partners." },
                        { "Problem-solving", @"Investigates complex issues to identify the root causes and impacts, assesses a range of solutions, and makes informed decisions on the best course of action, often in collaboration with other experts." },
                        { "Improvement mindset", @"Identifies and evaluates potential improvements to products, practices, or services.
Leads implementation of enhancements within own area of responsibility.
Assesses effectiveness of implemented changes." },
                        { "Creativity", @"Creatively applies innovative thinking and design practices in identifying solutions that will deliver value for the benefit of the customer/stakeholder." },
                        { "Communication", @"Communicates clearly with impact, articulating complex information and ideas to broad audiences with different viewpoints.
Leads and encourages conversations to share ideas and build consensus on actions to be taken." },
                        { "Leadership", @"Provides leadership at an operational level.
Implements and executes policies aligned to strategic plans.
Assesses and evaluates risk.
Takes all requirements into account when considering proposals." },
                        { "Adaptability", @"Leads adaptations to changing business environments.
Guides teams through transitions, maintaining focus on organisational objectives." },
                        { "Learning and development", @"Uses their skills and knowledge to help establish the standards that others in the organisation will apply.
Takes the initiative to develop a wider breadth of knowledge across industry and/or business and identify and manage development opportunities in area of responsibility." },
                        { "Digital mindset", @"Recognises and evaluates the organisational impact of new technologies and digital services.
Implements new and effective practices.
Advises on available standards, methods, tools, applications and processes relevant to group specialism(s) and can make appropriate choices from alternatives." },
                        { "Security, privacy and ethics", @"Contributes proactively to the implementation of professional working practices and helps promote a supportive organisational culture." },
                    }
                }
            },
            {
                6, new SfiaGenericLevelData
                {
                    Level = 6,
                    Essence = @"Has significant organisational influence, makes high-level decisions, shapes policies, demonstrates leadership, promotes organisational collaboration, and accepts accountability in key areas.",
                    GuidanceNotes = @"SFIA Levels represent levels of responsibility in the workplace. Each successive level describes increasing impact, responsibility and accountability.

Autonomy, influence and complexity are generic attributes that indicate the level of responsibility.
Business skills and behavioural factors describe the behaviours required to be effective at each level.
The knowledge attribute defines the depth and breadth of understanding required to perform and influence work effectively.
Understanding these attributes will help you get the most out of SFIA. They are critical to understanding and applying the levels described in the SFIA skill descriptions.",
                    CoreAttributes = new Dictionary<string, string>
                    {
                        { "Autonomy", @"Guides high level decisions and strategies within the organisation’s overall policies and objectives. Has defined authority and accountability for actions and decisions within a significant area of work, including technical, financial and quality aspects. Delegates responsibility for operational objectives." },
                        { "Influence", @"Influences the formation of strategy and the execution of business plans. Has a significant management level of contact with internal colleagues and external contacts. Has organisational leadership and influence over the appointment and management of resources related to the implementation of strategic initiatives." },
                        { "Complexity", @"Performs highly complex work activities covering technical, financial and quality aspects." },
                        { "Knowledge", @"Applies broad business knowledge to enable strategic leadership and decision-making across various domains." },
                    },
                    BusinessSkills = new Dictionary<string, string>
                    {
                        { "Decision-making", @"Uses judgement to make decisions that initiate the achievement of agreed strategic objectives including financial performance.
Escalates when broader strategic direction is impacted." },
                        { "Planning", @"Initiates and influences strategic objectives and assigns responsibilities." },
                        { "Collaboration", @"Leads collaboration with a diverse range of stakeholders across competing objectives within the organisation.
Builds strong, influential connections with key internal and external contacts at senior management/technical leader level" },
                        { "Problem-solving", @"Anticipates and leads in addressing problems and opportunities that may impact organisational objectives, establishing a strategic approach and allocating resources." },
                        { "Improvement mindset", @"Drives improvement initiatives that have a significant impact on the organisation.
Aligns improvement strategies with organisational objectives.
Engages stakeholders in improvement processes." },
                        { "Creativity", @"Creatively applies a wide range of new ideas and effective management techniques to achieve results that align with organisational strategy." },
                        { "Communication", @"Communicates with credibility at all levels across the organisation to broad audiences with divergent objectives.
Explains complex information and ideas clearly, influencing the strategic direction.
Promotes information sharing across the organisation." },
                        { "Leadership", @"Provides leadership at an organisational level.
Contributes to the development and implementation of policy and strategy.
Understands and communicates industry developments, and the role and impact of technology.
Manages and mitigates organisational risk.
Balances the requirements of proposals with the broader needs of the organisation." },
                        { "Adaptability", @"Drives organisational adaptability by initiating and leading significant changes. Influences change management strategies at an organisational level." },
                        { "Learning and development", @"Promotes the application of knowledge to support strategic imperatives.
Actively develops their strategic leadership and technical skills and leads the development of skills in their area of accountability." },
                        { "Digital mindset", @"Leads the enhancement of the organisation’s digital capabilities.
Identifies and endorses opportunities to adopt new technologies and digital services.
Leads digital governance and compliance with relevant legislation and the need for products and services." },
                        { "Security, privacy and ethics", @"Takes a leading role in promoting and ensuring appropriate culture and working practices, including the provision of equal access and opportunity to people with diverse abilities." },
                    }
                }
            },
            {
                7, new SfiaGenericLevelData
                {
                    Level = 7,
                    Essence = @"Operates at the highest organisational level, determines overall organisational vision and strategy, and assumes accountability for overall success.",
                    GuidanceNotes = @"SFIA Levels represent levels of responsibility in the workplace. Each successive level describes increasing impact, responsibility and accountability.

Autonomy, influence and complexity are generic attributes that indicate the level of responsibility.
Business skills and behavioural factors describe the behaviours required to be effective at each level.
The knowledge attribute defines the depth and breadth of understanding required to perform and influence work effectively.
Understanding these attributes will help you get the most out of SFIA. They are critical to understanding and applying the levels described in the SFIA skill descriptions.",
                    CoreAttributes = new Dictionary<string, string>
                    {
                        { "Autonomy", @"Defines and leads the organisation’s vision and strategy within over-arching business objectives. Is fully accountable for actions taken and decisions made, both by self and others to whom responsibilities have been assigned. Delegates authority and responsibility for strategic business objectives." },
                        { "Influence", @"Directs, influences and inspires the strategic direction and development of the organisation. Has an extensive leadership level of contact with internal colleagues and external contacts. Authorises the appointment of required resources." },
                        { "Complexity", @"Performs extensive strategic leadership in delivering business value through vision, governance and executive management." },
                        { "Knowledge", @"Applies strategic and broad-based knowledge to shape organisational strategy, anticipate future industry trends, and prepare the organisation to adapt and lead." },
                    },
                    BusinessSkills = new Dictionary<string, string>
                    {
                        { "Decision-making", @"Uses judgement in making decisions critical to the organisational strategic direction and success.
Escalates when business executive management input is required through established governance structures." },
                        { "Planning", @"Plans and leads at the highest level of authority over all aspects of a significant area of work." },
                        { "Collaboration", @"Drives collaboration, engaging with leadership stakeholders ensuring alignment to corporate vision and strategy.
Builds strong, influential relationships with customers, partners and industry leaders." },
                        { "Problem-solving", @"Manages inter-relationships between impacted parties and strategic imperatives, recognising the broader business context and drawing accurate conclusions when resolving problems." },
                        { "Improvement mindset", @"Defines and communicates the organisational approach to continuous improvement.
Cultivates a culture of ongoing enhancement.
Evaluates the impact of improvement initiatives on organisational success." },
                        { "Creativity", @"Champions creativity and innovation in driving strategy development to enable business opportunities." },
                        { "Communication", @"Communicates to audiences at all levels within own organisation and engages with industry.
Presents compelling arguments and ideas authoritatively and convincingly to achieve business objectives." },
                        { "Leadership", @"Leads strategic management.
Applies the highest level of leadership to the formulation and implementation of strategy.
Communicates the potential impact of emerging practices and technologies on organisations and individuals and assesses the risks of using or not using such practices and technologies.
Establishes governance to address business risk.
Ensures proposals align with the strategic direction of the organisation." },
                        { "Adaptability", @"Champions organisational agility and resilience.
Embeds adaptability into organisational culture and strategic planning." },
                        { "Learning and development", @"Inspires a learning culture to align with business objectives.
Maintains strategic insight into contemporary and emerging industry landscapes.
Ensures the organisation develops and mobilises the full range of required skills and capabilities." },
                        { "Digital mindset", @"Leads the development of the organisation’s digital culture and the transformational vision.
Advances capability and/or exploitation of technology within one or more organisations through a deep understanding of the industry and the implications of emerging technologies.
Accountable for assessing how laws and regulations impact organisational objectives and its use of digital, data and technology capabilities." },
                        { "Security, privacy and ethics", @"Provides clear direction and strategic leadership for embedding compliance, organisational culture, and working practices, and actively promotes diversity and inclusivity." },
                    }
                }
            }
        };

        public static string GetFullDescription(int level)
        {
            if (!Matrix.TryGetValue(level, out var data)) return string.Empty;
            var sb = new StringBuilder();
            sb.AppendLine($"Levels of responsibility: Level {level}");
            sb.AppendLine($"Essence of the level: {data.Essence}\n");
            if (!string.IsNullOrWhiteSpace(data.GuidanceNotes))
            {
                sb.AppendLine("Guidance notes");
                sb.AppendLine(data.GuidanceNotes.Replace("\n", "\n"));
                sb.AppendLine();
            }
            foreach (var kvp in data.CoreAttributes)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value))
                {
                    sb.AppendLine(kvp.Key);
                    sb.AppendLine(kvp.Value.Replace("\n", "\n"));
                    sb.AppendLine();
                }
            }
            sb.AppendLine("Business skills / Behavioural factors");
            foreach (var kvp in data.BusinessSkills)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value))
                {
                    sb.AppendLine(kvp.Key);
                    sb.AppendLine(kvp.Value.Replace("\n", "\n"));
                    sb.AppendLine();
                }
            }
            return sb.ToString().TrimEnd();
        }
    }
}