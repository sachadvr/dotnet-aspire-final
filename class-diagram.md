# Diagramme de classes - Base de données MyApp

```mermaid
classDiagram
    class Category {
        +int Id
        +string Name
        +string? Description
        +DateTime CreatedAt
        +ICollection~Product~ Products
    }

    class Product {
        +int Id
        +string Name
        +string Description
        +decimal Price
        +int Stock
        +string? ImageUrl
        +DateTime CreatedAt
        +int? CategoryId
        +Category? Category
        +ICollection~OrderItem~ OrderItems
    }

    class Order {
        +int Id
        +string UserId
        +string UserName
        +DateTime OrderDate
        +OrderStatus Status
        +decimal TotalAmount
        +string Address
        +string? PaymentLink
        +ICollection~OrderItem~ OrderItems
    }

    class OrderItem {
        +int Id
        +int OrderId
        +int ProductId
        +int Quantity
        +decimal UnitPrice
        +Order Order
        +Product Product
    }

    class OrderStatus {
        <<enumeration>>
        Pending
        Confirmed
        Shipped
        Delivered
        Cancelled
    }

    Category "1" --> "*" Product : "contient"
    Product "*" --> "*" OrderItem : "dans"
    Order "1" --> "*" OrderItem : "contient"
    Order --> OrderStatus : "utilise"
```

## Relations

- **Category → Product** : Une catégorie peut contenir plusieurs produits (relation 1-N)
- **Product → OrderItem** : Un produit peut être dans plusieurs lignes de commande (relation 1-N)
- **Order → OrderItem** : Une commande contient plusieurs lignes de commande (relation 1-N)
- **Order → OrderStatus** : Une commande utilise un statut (enum)

## Notes

- `CategoryId` dans `Product` est nullable, donc un produit peut ne pas avoir de catégorie
- `OrderItem` stocke le prix unitaire au moment de la commande (`UnitPrice`) pour préserver l'historique
- `Order.UserId` et `Order.UserName` stockent les informations utilisateur depuis Keycloak
