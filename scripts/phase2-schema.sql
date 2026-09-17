-- Run after initial migration if EF auto-migrate doesn't apply new schema
ALTER TABLE drugs ADD COLUMN IF NOT EXISTS is_active TINYINT(1) NOT NULL DEFAULT 1;

CREATE TABLE IF NOT EXISTS drug_batches (
    batch_id INT AUTO_INCREMENT PRIMARY KEY,
    drug_id VARCHAR(50) NOT NULL,
    batch_number VARCHAR(100) NOT NULL,
    manufacture_date DATETIME NULL,
    expiry_date DATETIME NOT NULL,
    quantity INT NOT NULL DEFAULT 0,
    UNIQUE KEY uk_drug_batch (drug_id, batch_number),
    CONSTRAINT fk_batch_drug FOREIGN KEY (drug_id) REFERENCES drugs(drug_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS purchase_orders (
    order_id INT AUTO_INCREMENT PRIMARY KEY,
    supplier_id INT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Draft',
    created_by VARCHAR(100) NOT NULL,
    created_at DATETIME NOT NULL,
    approved_at DATETIME NULL,
    CONSTRAINT fk_po_supplier FOREIGN KEY (supplier_id) REFERENCES suppliers(supplier_id) ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS purchase_order_lines (
    line_id INT AUTO_INCREMENT PRIMARY KEY,
    order_id INT NOT NULL,
    drug_id VARCHAR(50) NOT NULL,
    quantity INT NOT NULL,
    received_quantity INT NOT NULL DEFAULT 0,
    CONSTRAINT fk_pol_order FOREIGN KEY (order_id) REFERENCES purchase_orders(order_id) ON DELETE CASCADE,
    CONSTRAINT fk_pol_drug FOREIGN KEY (drug_id) REFERENCES drugs(drug_id) ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS audit_logs (
    audit_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    entity_type VARCHAR(100) NOT NULL,
    entity_id VARCHAR(100) NOT NULL,
    action VARCHAR(50) NOT NULL,
    user_name VARCHAR(100) NOT NULL,
    old_values LONGTEXT NULL,
    new_values LONGTEXT NULL,
    created_at DATETIME NOT NULL
);

-- Add FK drugs -> suppliers if not exists (may fail if orphan data)
-- ALTER TABLE drugs ADD CONSTRAINT fk_drugs_supplier FOREIGN KEY (supplier_id) REFERENCES suppliers(supplier_id) ON DELETE RESTRICT;
