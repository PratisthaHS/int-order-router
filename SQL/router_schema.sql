CREATE TABLE customers (
    id INT PRIMARY KEY IDENTITY(1,1),
    name VARCHAR(255) NOT NULL UNIQUE,
    created_at DATETIME DEFAULT GETDATE()
);

CREATE TABLE lane_routing_rules (
    id INT PRIMARY KEY IDENTITY(1,1),
    customer_id INT NOT NULL,
    pick_up_city VARCHAR(255) NOT NULL,
    drop_off_city VARCHAR(255) NOT NULL,
    route_to VARCHAR(255) DEFAULT 'Trinium',
    is_active BIT DEFAULT 1,
    created_by VARCHAR(255),
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (customer_id) REFERENCES customers(id)
);

CREATE TABLE weekly_quota_rules (
    id INT PRIMARY KEY IDENTITY(1,1),
    customer_id INT NOT NULL,
    weekly_quota INT NOT NULL,
    start_of_week DATE NOT NULL,
    is_active BIT DEFAULT 1,
    created_by VARCHAR(255),
    created_at DATETIME DEFAULT GETDATE(),
    updated_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (customer_id) REFERENCES customers(id)
);

CREATE TABLE routing_history (
    id INT PRIMARY KEY IDENTITY(1,1),
    customer_id INT NOT NULL,
    mbol VARCHAR(255),
    booking_number VARCHAR(255),
    container_id VARCHAR(255),
    pick_up_city VARCHAR(255),
    drop_off_city VARCHAR(255),
    routed_to VARCHAR(255),
    routed_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (customer_id) REFERENCES customers(id)
);

CREATE TABLE lane_rule_audit_log (
    id INT PRIMARY KEY IDENTITY(1,1),
    route_id INT NOT NULL,
    rule_id INT NOT NULL,
    notes VARCHAR(1000),
    logged_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (route_id) REFERENCES routing_history(id),
    FOREIGN KEY (rule_id) REFERENCES lane_routing_rules(id)
);

CREATE TABLE quota_rule_audit_log (
    id INT PRIMARY KEY IDENTITY(1,1),
    route_id INT NOT NULL,
    rule_id INT NOT NULL,
    notes VARCHAR(1000),
    logged_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (route_id) REFERENCES routing_history(id),
    FOREIGN KEY (rule_id) REFERENCES weekly_quota_rules(id)
);
