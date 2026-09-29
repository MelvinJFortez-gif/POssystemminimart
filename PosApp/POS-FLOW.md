# Mini Mart POS System Flow

This chart follows the current Windows desktop POS application and its MySQL database.

## Cashier sale flow

```mermaid
flowchart TD
    A([Start POS]) --> B[Connect to MySQL and initialize POS]
    B --> C{Database available?}
    C -- No --> C1[Show connection error and stop]
    C -- Yes --> D{Any user accounts yet?}
    D -- No --> D1[Create the initial admin account]
    D -- Yes --> E[Ask for username and password]
    D1 --> E
    E --> F{Credentials valid?}
    F -- No --> G[Show invalid credentials]
    G --> H{Fewer than 3 attempts?}
    H -- Yes --> E
    H -- No --> Z([Exit])
    F -- Yes --> I[Open POS screen and load products from MySQL]

    I --> J{How is an item selected?}
    J -- Scan barcode --> K[Look up product by barcode]
    J -- Click product tile --> L[Select displayed product]
    K --> M{Product found and stock available?}
    L --> M
    M -- No --> N[Show unknown item or out-of-stock message]
    N --> J
    M -- Yes --> O[Add item or increase its cart quantity]
    O --> P[Update cart and total]
    P --> Q{Continue editing cart?}
    Q -- Yes --> R[Change quantity, remove item, or clear cart]
    R --> P
    Q -- No --> S{Choose payment}

    S -- Cash --> T[Enter amount tendered]
    T --> U{Amount covers total?}
    U -- No --> V[Show invalid or insufficient amount]
    V --> T
    U -- Yes --> W[Set cash paid amount and calculate change]
    S -- Card --> X[Set card paid amount equal to total]
    W --> Y[Save sale in one database transaction]
    X --> Y
    Y --> AA{Transaction succeeds?}
    AA -- No --> AB[Roll back sale, items, and stock changes; show error]
    AB --> J
    AA -- Yes --> AC[Commit sale, sale items, and stock deduction]
    AC --> AD[Send receipt to POS-58 printer; include drawer command for cash]
    AD --> AE{Printer available?}
    AE -- No --> AF[Warn that sale is saved but receipt and drawer command failed]
    AE -- Yes --> AG[Print receipt and cut paper]
    AG --> AH{Cash sale?}
    AH -- Yes --> AI[Open cash drawer and show change]
    AH -- No --> AJ[Finish checkout]
    AI --> AJ
    AF --> AJ
    AJ --> AK[Clear cart and reload product stock]
    AK --> J
```

## Product maintenance

The cashier can use **Product (F9)** to add a product or update the product matching a barcode. Saving inserts or updates its name, category, price, and stock in MySQL; the product list and tiles are then reloaded.

```mermaid
flowchart LR
    A[Choose Product] --> B[Scan or enter barcode]
    B --> C{Barcode already exists?}
    C -- Yes --> D[Load existing product values]
    C -- No --> E[Start with blank product values]
    D --> F[Enter name, category, price, and stock]
    E --> F
    F --> G{Price and stock are valid numbers?}
    G -- No --> H[Show invalid number message]
    H --> F
    G -- Yes --> I[Save product to MySQL by barcode]
    I --> J[Reload products and product tiles]
```

## What the database stores

- **Users**: login name, password hash, and role.
- **Products**: barcode, name, category, price, and available stock.
- **Sales**: cashier, total, amount paid, change, payment method, and sale time.
- **Sale items**: each product, quantity, and price recorded for a sale.

Each checkout is atomic: the sale record, sale-item records, and stock deductions commit together. If stock is insufficient during checkout or a database operation fails, the transaction is rolled back.
