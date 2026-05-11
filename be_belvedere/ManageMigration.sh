#!/usr/bin/env bash

MIGRATION_PROJECT="./belvedere.Persistence/belvedere.Persistence.csproj"
STARTUP_PROJECT="./belvedere/belvedere.csproj"

# Define regex for migration names to skip the [dbg] logs and select the migration by checking the date at the beginning
MIGRATION_NAME_REGEX="^[0-9]{14}_.+"

add_migration() {
    local migration_name="$1"

    if [ -z "$migration_name" ]; then
        migration_name="Initial"
    fi

    echo -e "\nAdding migration '$migration_name'..."
    dotnet ef migrations add "$migration_name" \
        --project "$MIGRATION_PROJECT" \
        --startup-project "$STARTUP_PROJECT"
}

update_database() {
    echo -e "\nUpdating the database..."
    dotnet ef database update \
        --project "$MIGRATION_PROJECT" \
        --startup-project "$STARTUP_PROJECT"
}

list_migrations() {
    echo -e "\nAvailable migrations:"
    echo "----------------------------------"
    
    # Store migrations in an array
    migrations=()
    while read -r line; do
        if [[ "$line" =~ $MIGRATION_NAME_REGEX ]]; then
            migrations+=("$line")
        fi
    done < <(dotnet ef migrations list --project "$MIGRATION_PROJECT" --startup-project "$STARTUP_PROJECT" 2>/dev/null)

    local i=0
    for migration in "${migrations[@]}"; do
        echo "$i. $migration"
        ((i++))
    done

    if [ $i -eq 0 ]; then
        echo "No migrations found."
    fi
    echo "----------------------------------"
}

script_migration() {
    local from_migration="${1:-0}"
    local to_migration="$2"
    local output_path="${3:-./migration.sql}"

    if [ -z "$to_migration" ]; then
        # Use list_migrations logic to get the last migration
        to_migration=$(dotnet ef migrations list --project "$MIGRATION_PROJECT" --startup-project "$STARTUP_PROJECT" 2>/dev/null | grep -E "$MIGRATION_NAME_REGEX" | tail -n 1)
    fi

    echo -e "\nGenerating SQL script from migration '$from_migration' to '$to_migration'..."
    dotnet ef migrations script "$from_migration" "$to_migration" \
        --project "$MIGRATION_PROJECT" \
        --startup-project "$STARTUP_PROJECT" \
        --output "$output_path"

    echo "SQL script written to $output_path"
}

echo "=================================="
echo "   EF Core Migration Management   "
echo "=================================="
echo "1) Add Migration"
echo "2) Update Database"
echo "3) Add + Update"
echo "4) Generate SQL Migration Script"
echo "5) List Migrations"
echo "----------------------------------"

read -p "Please enter 1, 2, 3, 4 or 5: " choice

case $choice in
    1)
        read -p "Enter the migration name (leave blank for 'Initial'): " migration_name
        add_migration "$migration_name"
        ;;
    2)
        update_database
        ;;
    3)
        read -p "Enter the migration name (leave blank for 'Initial'): " migration_name
        add_migration "$migration_name"
        if [ $? -eq 0 ]; then
            echo "--------------------------------------------------------------------"
            update_database
        else
            echo "--------------------------------------------------------------------"
            echo "Migration failed. Database update skipped."
        fi
        ;;
    4)
        echo -e "\nGenerating SQL Migration Script..."
        list_migrations
        read -p "Enter the start migration name (leave blank for '0'): " from_migration
        read -p "Enter the target migration name (leave blank for latest): " to_migration
        read -p "Enter output SQL file path (leave blank for './migration.sql'): " output_path
        
        if [ -z "$from_migration" ]; then
            from_migration="0"
        fi
        
        if [ -z "$output_path" ]; then
            output_path="./migration.sql"
        fi
        
        script_migration "$from_migration" "$to_migration" "$output_path"
        ;;
    5)
        list_migrations
        ;;
    *)
        echo -e "\nInvalid selection. Exiting..."
        ;;
esac

echo -e "\nDone!"
