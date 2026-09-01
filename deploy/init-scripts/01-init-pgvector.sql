-- pgvector eklentisini aktifleştir
CREATE EXTENSION IF NOT EXISTS vector;

-- Analiz ve PR oturumları tablosu
CREATE TABLE IF NOT EXISTS pull_request_analyses (
    id VARCHAR(64) PRIMARY KEY,
    repository_url VARCHAR(255) NOT NULL,
    pr_id VARCHAR(64) NOT NULL,
    language VARCHAR(32) NOT NULL,
    status VARCHAR(32) NOT NULL,
    cyclomatic_complexity INT,
    lines_of_code INT,
    violation_count INT,
    ai_enhanced BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Bulunan Kural İhlalleri tablosu
CREATE TABLE IF NOT EXISTS code_violations (
    id SERIAL PRIMARY KEY,
    analysis_id VARCHAR(64) REFERENCES pull_request_analyses(id) ON DELETE CASCADE,
    rule_id VARCHAR(64) NOT NULL,
    rule_name VARCHAR(128) NOT NULL,
    category VARCHAR(64) NOT NULL,
    severity VARCHAR(32) NOT NULL,
    file_path VARCHAR(255) NOT NULL,
    start_line INT,
    end_line INT,
    description TEXT,
    suggested_fix TEXT
);

-- Otonom Ajan Tartışmaları ve Uzlaşı Kaydı
CREATE TABLE IF NOT EXISTS agent_debates (
    id SERIAL PRIMARY KEY,
    analysis_id VARCHAR(64) REFERENCES pull_request_analyses(id) ON DELETE CASCADE,
    reviewer_opinion TEXT,
    security_opinion TEXT,
    qa_opinion TEXT,
    consensus_summary TEXT,
    generated_test_code TEXT,
    suggested_patch TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Vektör tabanlı Kurumsal Mimari Kuralları (Semantic Search için)
CREATE TABLE IF NOT EXISTS architecture_rules_embeddings (
    id SERIAL PRIMARY KEY,
    rule_name VARCHAR(128) NOT NULL,
    category VARCHAR(64) NOT NULL,
    rule_description TEXT NOT NULL,
    embedding vector(1536) -- OpenAI / Gemini embedding vektörü
);
