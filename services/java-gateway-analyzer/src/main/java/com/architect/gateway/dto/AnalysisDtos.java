package com.architect.gateway.dto;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.List;

public class AnalysisDtos {

    @Data
    @Builder
    @NoArgsConstructor
    @AllArgsConstructor
    public static class RequestDto {
        private String requestId;
        private String repositoryUrl;
        private String pullRequestId;
        private String filePath;
        private String sourceCode;
        private String language; // "JAVA" or "CSHARP"
        private boolean enableAiAgents;
    }

    @Data
    @Builder
    @NoArgsConstructor
    @AllArgsConstructor
    public static class ViolationDto {
        private String ruleId;
        private String ruleName;
        private String category;
        private String severity;
        private int startLine;
        private int endLine;
        private String description;
        private String suggestedFix;
    }

    @Data
    @Builder
    @NoArgsConstructor
    @AllArgsConstructor
    public static class MetricsDto {
        private int linesOfCode;
        private int cyclomaticComplexity;
        private int maintainabilityIndex;
        private int methodCount;
        private int classCount;
    }

    @Data
    @Builder
    @NoArgsConstructor
    @AllArgsConstructor
    public static class ResponseDto {
        private String requestId;
        private String filePath;
        private boolean success;
        private String errorMessage;
        private MetricsDto metrics;
        private List<ViolationDto> violations;
        private long executionTimeMs;
    }
}
