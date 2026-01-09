using System;
using System.Collections.Generic;
using System.Text;

namespace IIM.Application.Workflows
{
	public class QuickAssessmentHandler
	{
		private const string SchemaAnalysisPrompt = """
            You are a senior data architect specializing in document intelligence systems. Your expertise includes JSON schema analysis, semantic search optimization, and designing question sets that maximize retrieval accuracy in vector databases.

            Analyze the following JSON schema for a Project Charter document. Your goal is to generate a comprehensive set of natural language questions that will be embedded in Qdrant to enable semantic search against charter documents.
            json
            {0}


            **Instructions:**

            1. **Schema Analysis** — Identify all top-level sections and their nested properties. Note data types, required fields, and cross-section relationships.

            2. **Section Mapping** — For each logical section, provide:
               - Section path (e.g., `projectOverview.projectName`)
               - Data type and constraints
               - Investigative purpose (what this field reveals)

            3. **Question Generation** — For each field or logical grouping, generate 3-5 question variations that:
               - Use different phrasings (formal, conversational, keyword-style)
               - Cover both direct lookups ("What is the project name?") and analytical queries ("How does the timeline align with budget phases?")
               - Include relationship questions that span sections where applicable

            4. **Intent Tagging** — Classify each question with one or more intents:
               - `FactLookup` — Direct field retrieval
               - `RelationshipAnalysis` — Cross-reference between sections
               - `RiskAssessment` — Risk/compliance focused
               - `TimelineQuery` — Date/milestone related
               - `StakeholderQuery` — People/roles focused

            **Output Format:**

            Respond ONLY with valid JSON matching this structure:

            {
              "sections": [
                {
                  "path": "projectOverview",
                  "description": "Core project identification and scope",
                  "fields": [
                    {
                      "path": "projectOverview.projectName",
                      "type": "string",
                      "purpose": "Unique identifier for the project",
                      "questions": [
                        {"text": "What is the project name?", "intents": ["FactLookup"]},
                        {"text": "Which project is this charter for?", "intents": ["FactLookup"]},
                        {"text": "project name", "intents": ["FactLookup"]}
                      ]
                    }
                  ]
                }
              ],
              "crossSectionQuestions": [
                {
                  "text": "How do the identified risks impact the compliance requirements?",
                  "intents": ["RelationshipAnalysis", "RiskAssessment"],
                  "relatedPaths": ["projectRisks", "complianceSignoff"]
                }
              ]
            }

            Do not include markdown code fences or any text outside the JSON object.
            """;

		private const string JsonSchema = """
            {
              "$schema": "http://json-schema.org/draft-07/schema#",
              "title": "Project Charter Schema",
              "description": "Schema for validating project charter data.",
              "type": "object",
              "properties": {
                "projectOverview": {
                  "type": "object",
                  "properties": {
                    "projectName": { "type": "string" },
                    "description": { "type": "string" },
                    "programArea": { "type": "string" },
                    "businessFunction": { "type": "string" },
                    "dataSensitivity": { "type": "string", "enum": ["Low", "Medium", "High"] },
                    "sensitivityReportReference": { "type": ["string", "null"] },
                    "dataTypes": {
                      "type": "array",
                      "items": { "type": "string" }
                    },
                    "businessCritical": { "type": "boolean" },
                    "businessCriticalImpact": { "type": "string" }
                  },
                  "required": ["projectName", "description", "businessCritical"]
                },
                "operationalConsiderations": {
                  "type": "object",
                  "properties": {
                    "stakeholderConsultation": { "type": "boolean" },
                    "acceptanceCriteriaDefined": { "type": "string" },
                    "testingConducted": { "type": "string" },
                    "responsibleParty": { "type": "string" },
                    "incidentResponseProcedures": { "type": "boolean" }
                  },
                  "required": ["acceptanceCriteriaDefined"]
                },
                "systemArchitecture": {
                  "type": "object",
                  "properties": {
                    "architectureType": { "type": "string" },
                    "redundancy": { "type": "boolean" },
                    "redundancyDetails": { "type": ["string", "null"] },
                    "singlePointOfFailureAvoided": { "type": "string" },
                    "hostingLocation": { "type": "string" },
                    "availabilityRequirements": { "type": "string" },
                    "projectTimeline": {
                      "type": "array",
                      "items": {
                        "type": "object",
                        "properties": {
                          "milestone": { "type": "string" },
                          "date": { "type": "string", "format": "date" }
                        },
                        "required": ["milestone", "date"]
                      }
                    }
                  },
                  "required": ["architectureType"]
                },
                "projectBudget": {
                  "type": "object",
                  "properties": {
                    "totalBudget": { "type": "number" },
                    "currency": { "type": "string" },
                    "breakdown": {
                      "type": "object",
                      "properties": {
                        "development": { "type": "number" },
                        "marketing": { "type": "number" },
                        "testing": { "type": "number" }
                      },
                      "required": ["development", "marketing", "testing"]
                    }
                  },
                  "required": ["totalBudget", "currency", "breakdown"]
                },
                "projectRisks": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "risk": { "type": "string" },
                      "mitigation": { "type": "string" }
                    },
                    "required": ["risk", "mitigation"]
                  }
                },
                "complianceSignoff": {
                  "type": "object",
                  "properties": {
                    "adheresToPolicies": { "type": "boolean" },
                    "assessorName": { "type": ["string", "null"] },
                    "assessmentDate": { "type": ["string", "null"] },
                    "signatures": {
                      "type": "object",
                      "properties": {
                        "assessor": { "type": ["string", "null"] },
                        "systemOwner": { "type": "string" },
                        "programManager": { "type": "string" }
                      },
                      "required": ["systemOwner", "programManager"]
                    }
                  },
                  "required": ["adheresToPolicies", "signatures"]
                }
              },
              "required": ["projectOverview", "operationalConsiderations", "systemArchitecture", "complianceSignoff"]
            }
            """;

		public async Task Handle()
		{
			var prompt = string.Format(SchemaAnalysisPrompt, JsonSchema);
			// Use prompt with your LLM client
		}
	}
}