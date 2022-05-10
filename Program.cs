using System;
using NLog.Web;
using System.IO;
using System.Linq;
using nwconsole.Model;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace nwconsole
{
    class Program
    {
        // create static instance of Logger
        private static NLog.Logger logger = NLogBuilder.ConfigureNLog(Directory.GetCurrentDirectory() + "\\nlog.config").GetCurrentClassLogger();
        static void Main(string[] args)
        {
            logger.Info("Program started");

            try
            {
                string choice;
                do
                {
                    Console.WriteLine("1) Add Product");
                    Console.WriteLine("2) Edit a Product");
                    Console.WriteLine("3) Delete Product");
                    Console.WriteLine("4) Display Product(s)");
                    Console.WriteLine("5) Add Category");
                    Console.WriteLine("6) Edit a Category");
                    Console.WriteLine("7) Delete Category");
                    Console.WriteLine("8) Display Category(-ies)");

                    Console.WriteLine("\"q\" to quit");
                    choice = Console.ReadLine();
                    Console.Clear();
                    logger.Info($"User chose/selected option {choice}");
                    // Add product
                    if (choice == "1") {
                        // asks and grabs category from db
                        Console.WriteLine("What category would this product be under?");
                        var db = new Northwind_48_BTCContext();
                        var query = db.Categories.OrderBy(c => c.CategoryId);
                        Console.WriteLine($"Found {query.Count()} records");
                        // makes list with gathered categories
                        int number = 1;
                        foreach (var categoryName in query) {
                            Console.WriteLine($"{number}) {categoryName.CategoryName}");
                            number++;
                        }
                        // choice from results above
                        int categoryChoice = int.Parse(Console.ReadLine());
                        Console.Clear();
                        string categoryChosen = query.ToList().ElementAt(categoryChoice-1).CategoryName;
                        logger.Info($"User chose {categoryChosen}");
                        Console.WriteLine($"Selected: {categoryChosen}");
                        if (categoryChoice >= 1 && categoryChoice <= number) {
                            var product = new Product();
                            product.CategoryId = query.ToList().ElementAt(categoryChoice-1).CategoryId;

                            Console.WriteLine("Enter new product name");
                            string pName = Console.ReadLine();
                            product.ProductName = pName;

                            Console.WriteLine("Enter unit price");
                            int pUnitPrice = int.Parse(Console.ReadLine()); 
                            product.UnitPrice = pUnitPrice;

                            Console.WriteLine("Is product discontinued? (y/n)");
                            string pdiscontinued = Console.ReadLine().ToLower();
                            if (pdiscontinued == "y") {
                                product.Discontinued = true;
                            } else if (pdiscontinued == "n") {
                                product.Discontinued = false;
                            }

                            ValidationContext context = new ValidationContext(product, null, null);
                            List<ValidationResult> results = new List<ValidationResult>();

                            var isValid = Validator.TryValidateObject(product, context, results, true);
                            if (isValid)
                            {
                                db = new Northwind_48_BTCContext();
                                // check for unique name
                                if (db.Products.Any(p => p.ProductName == product.ProductName))
                                {
                                    // generate validation error
                                    isValid = false;
                                    results.Add(new ValidationResult("Product name exists", new string[] { "Name" }));
                                }
                                else
                                {
                                    logger.Info("Validation passed");
                                    // save product to db
                                    db.Products.Add(product);
                                    db.SaveChanges();
                                    logger.Info("Product added - {name}", product.ProductName);
                                }
                            }
                            if (!isValid)
                            {
                                foreach (var result in results)
                                {
                                    logger.Error($"{result.MemberNames.First()} : {result.ErrorMessage}");
                                }
                            }
                        } else {
                            Console.WriteLine("Please pick a valid choice in range.");
                        }
                    } else if (choice == "2") {
                        // Edit existing product
                        var db = new Northwind_48_BTCContext();
                        var query = db.Categories.OrderBy(p => p.CategoryId);

                        Console.WriteLine("Select the category your item is in:");
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        foreach (var item in query) {
                            Console.WriteLine($"{item.CategoryId}) {item.CategoryName}");
                        }
                        Console.ForegroundColor = ConsoleColor.White;
                        int categoryIdSelected = int.Parse(Console.ReadLine());
                        if (categoryIdSelected > 0 && categoryIdSelected <= query.Count()) {
                            // select the product
                            Console.WriteLine("Select the product you wish to edit:");
                            query = db.Categories.Where(c => c.CategoryId == categoryIdSelected).Include("Products").OrderBy(p => p.CategoryId);
                            int tmpNum = 1;
                            foreach (var item in query) {
                                Console.WriteLine($"{item.CategoryName}");
                                foreach (Product p in item.Products) {
                                    Console.WriteLine($"\t{tmpNum}) {p.ProductName}");
                                    tmpNum++;
                                }
                            }
                            int productSelected = int.Parse(Console.ReadLine());
                            Product productEdit = db.Products.Where(p => p.CategoryId == categoryIdSelected)
                                                             .ToList()
                                                             .ElementAt(productSelected-1);
                            int productId = productEdit.ProductId;
                            Console.Clear();
                            Console.WriteLine($"Selected \"{productEdit.ProductName}\"");
                            Console.WriteLine("1) Edit Name");
                            Console.WriteLine("2) Edit Price");
                            Console.WriteLine("3) Edit Category");
                            Console.WriteLine("4) Edit Discontinued");

                            string pechoice = Console.ReadLine();
                            Console.Clear();
                            if (pechoice == "1") {
                                Console.WriteLine("Enter new name");
                                string npName = Console.ReadLine();
                                db.Products.Where(p => p.ProductId == productId)
                                                             .ToList()
                                                             .First().ProductName = npName;
                                db.SaveChanges();
                            } else if (pechoice == "2") {
                                Console.WriteLine("Enter new price");
                                int npPrice = int.Parse(Console.ReadLine());
                                db.Products.Where(p => p.ProductId == productId)
                                                             .ToList()
                                                             .First().UnitPrice = npPrice;
                                db.SaveChanges();
                            } else if (pechoice == "3") {
                                db = new Northwind_48_BTCContext();
                                query = db.Categories.OrderBy(p => p.CategoryId);

                                Console.WriteLine("Select new category");
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                                tmpNum = 1;
                                foreach (var item in query) {
                                    Console.WriteLine($"{tmpNum}) {item.CategoryName}");
                                    tmpNum++;
                                }
                                Console.ForegroundColor = ConsoleColor.White;
                                int npCategory = int.Parse(Console.ReadLine());
                                if (npCategory > 0 && npCategory <= tmpNum) {
                                    Category category = query.ToList()[npCategory-1];
                                    if (category.CategoryId != productEdit.CategoryId) {
                                        Console.WriteLine($"{productEdit.CategoryId} {category.CategoryId}");
                                        logger.Info($"Transfered \"{productEdit.ProductName}\" from {productEdit.Category.CategoryName} to {category.CategoryName}");
                                        db.Products.Where(p => p.ProductId == productId)
                                                             .ToList()
                                                             .First()
                                                             .CategoryId = category.CategoryId;
                                        db.SaveChanges();
                                    } else {
                                        Console.WriteLine("New category cannot be the same");
                                    }
                                }
                            } else if (pechoice == "4") {
                                Console.WriteLine("Is the product discontinued? (y/n)");
                                string npdiscontinued = Console.ReadLine();
                                if (npdiscontinued == "y") {
                                    db.Products.Where(p => p.ProductId == productId)
                                                             .ToList()
                                                             .First()
                                                             .Discontinued = true;
                                    db.SaveChanges();
                                } else if (npdiscontinued == "n") {
                                    db.Products.Where(p => p.ProductId == productId)
                                                             .ToList()
                                                             .First()
                                                             .Discontinued = false;
                                    db.SaveChanges();
                                } else {
                                    Console.WriteLine("Input invalid. Edit aborted.");
                                }
                            } else {
                                Console.WriteLine("That is not an option.");
                            }
                        } else {
                            Console.WriteLine("Please enter a number within the expected range.");
                        }
                    } else if (choice == "3") { 
                        var db = new Northwind_48_BTCContext();
                        var query = db.Categories.OrderBy(p => p.CategoryId);

                        Console.WriteLine("Select the category your item is in:");
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        foreach (var item in query) {
                            Console.WriteLine($"{item.CategoryId}) {item.CategoryName}");
                        }
                        Console.ForegroundColor = ConsoleColor.White;
                        int categoryIdSelected = int.Parse(Console.ReadLine());
                        if (categoryIdSelected > 0 && categoryIdSelected <= query.Count()) {
                            Console.WriteLine("Select the product you'd like to delete");
                            var query2 = db.Products.Where(p => p.CategoryId == categoryIdSelected);
                            int tmpNum = 1;
                            foreach (var item in query2) {
                                Console.WriteLine($"{tmpNum}) {item.ProductName}");
                                tmpNum++;
                            }
                            int pdchoice = int.Parse(Console.ReadLine());
                            if (pdchoice > 0 && pdchoice <= tmpNum) {
                                Console.Clear();
                                Console.WriteLine($"Are you sure you want to delete (y/n): \"{query2.ToList()[pdchoice-1].ProductName}\"");
                                string confirmdeletion = Console.ReadLine().ToLower();
                                if (confirmdeletion == "y") {
                                    logger.Info($"User deleted \"{query2.ToList()[pdchoice-1].ProductName}\"");
                                    db.Products.Remove(query2.ToList()[pdchoice-1]);
                                    db.SaveChanges();
                                    Console.WriteLine("Item Deleted");
                                } else if (confirmdeletion == "n") {
                                    Console.WriteLine("Item will not be deleted");
                                } else {
                                    Console.WriteLine("Invalid input. Deletion aborted.");
                                }
                            } else {
                                Console.WriteLine("Number is out of range");
                            }
                        }
                    } else if (choice == "4") { 
                        Console.WriteLine("What products would you like to display?");
                        Console.WriteLine("1) Display All");
                        Console.WriteLine("2) Discontinued products");
                        Console.WriteLine("3) Active products");
                        string pdchoice = Console.ReadLine();

                        if (pdchoice == "1") {
                            logger.Info("User selected display all products");
                            // display all Products
                            var db = new Northwind_48_BTCContext();
                            var query = db.Categories.OrderBy(p => p.CategoryId)
                                                     .Include("Products").OrderBy(p => p.CategoryId);

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"{query.Count()} records returned");
                            foreach (var category in query) {
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine($"{category.CategoryName}");
                                foreach (Product p in category.Products) {
                                    if (p.Discontinued) {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                    } else {
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                    }
                                    Console.WriteLine($"\t{p.ProductName}");
                                }
                            }
                            Console.ForegroundColor = ConsoleColor.White;
                        } else if (pdchoice == "2") {
                            logger.Info("User selected display discontinued products");
                            // display discontinued Products
                            var db = new Northwind_48_BTCContext();
                            var query = db.Products.Where(p => p.Discontinued)
                                                    .OrderBy(p => p.CategoryId);

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"{query.Count()} records returned");
                            Console.ForegroundColor = ConsoleColor.Red;
                            foreach (var item in query) {
                                Console.WriteLine($"{item.ProductName}");
                            }
                            Console.ForegroundColor = ConsoleColor.White;
                        } else if (pdchoice == "3") {
                            logger.Info("User selected display active products");
                            // display active Products
                            var db = new Northwind_48_BTCContext();
                            var query = db.Products.Where(p => p.Discontinued == false)
                                                    .OrderBy(p => p.CategoryId);

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"{query.Count()} records returned");
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            foreach (var item in query) {
                                Console.WriteLine($"{item.ProductName}");
                            }
                            Console.ForegroundColor = ConsoleColor.White;
                        } else {
                            Console.WriteLine("That is not one of the 3 choices");
                        }
                    } else if (choice == "5") { 
                        Console.WriteLine("What is the name of the new category?");
                        string cName = Console.ReadLine();
                        Category category = new Category() {CategoryName = cName};

                        Console.WriteLine("What is the category description?");
                        string cDescription = Console.ReadLine();
                        category.Description = cDescription;
                        
                        ValidationContext context = new ValidationContext(category, null, null);
                        List<ValidationResult> results = new List<ValidationResult>();

                        var isValid = Validator.TryValidateObject(category, context, results, true);
                        if (isValid) {
                            var db = new Northwind_48_BTCContext();
                            // check for unique name
                            if (db.Categories.Any(c => c.CategoryName == category.CategoryName)) {
                                // generate validation error
                                isValid = false;
                                results.Add(new ValidationResult("Product name exists", new string[] { "Name" }));
                            } else {
                                logger.Info("Validation passed");
                                // save category to db
                                db.Categories.Add(category);
                                db.SaveChanges();
                                logger.Info("Category added - {name}", category.CategoryName);
                            }
                        } 
                        if (!isValid) {
                            foreach (var result in results) {
                                logger.Error($"{result.MemberNames.First()} : {result.ErrorMessage}");
                            }
                        }
                    } else if (choice == "6") { 
                        var db = new Northwind_48_BTCContext();
                        var query = db.Categories.OrderBy(p => p.CategoryId);

                        Console.WriteLine("Select the category you'd like to edit:");
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        int tmpNum = 1;
                        foreach (var item in query) {
                            Console.WriteLine($"{tmpNum}) {item.CategoryName}");
                            tmpNum++;
                        }
                        Console.ForegroundColor = ConsoleColor.White;
                        int categoryIdSelected = int.Parse(Console.ReadLine());
                        Console.Clear();
                        if (categoryIdSelected > 0 && categoryIdSelected <= tmpNum) {
                            Console.WriteLine("1) Change name");
                            Console.WriteLine("2) Change Description");
                            string cechoice = Console.ReadLine(); 

                            if (cechoice == "1") {
                                Console.WriteLine("What is the new name?");
                                string cNewName = Console.ReadLine();
                                logger.Info($"Changed Name from \"{query.ToList()[categoryIdSelected-1].CategoryName}\" to \"{cNewName}\"");
                                Console.WriteLine($"Changed Name from \"{query.ToList()[categoryIdSelected-1].CategoryName}\" to \"{cNewName}\"");
                                db.Categories.ToList()[categoryIdSelected-1].CategoryName = cNewName;
                                db.SaveChanges();
                            } else if (cechoice == "2") {
                                Console.WriteLine("What is the new description?");
                                string cNewDescription = Console.ReadLine();
                                logger.Info($"Changed Desc from \"{query.ToList()[categoryIdSelected-1].Description}\" to \"{cNewDescription}\"");
                                Console.WriteLine($"Changed Desc from \"{query.ToList()[categoryIdSelected-1].Description}\" to \"{cNewDescription}\"");
                                db.Categories.ToList()[categoryIdSelected-1].Description = cNewDescription;
                                db.SaveChanges();
                            } else {
                                Console.WriteLine("That is not an option");
                            }
                        }
                    } else if (choice == "7") { 
                        var db = new Northwind_48_BTCContext();
                        var query = db.Categories.OrderBy(p => p.CategoryId);

                        Console.WriteLine("Select the category you'd like to delete:");
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        int tmpNum = 1;
                        foreach (var item in query) {
                            Console.WriteLine($"{tmpNum}) {item.CategoryName}");
                            tmpNum++;
                        }
                        Console.ForegroundColor = ConsoleColor.White;
                        int cidtmp = int.Parse(Console.ReadLine());
                        if (cidtmp > 0 && cidtmp <= tmpNum) {
                            int categoryIdSelected = query.ToList().ElementAt(cidtmp-1).CategoryId;
                            var query2 = db.Products.Where(p => p.CategoryId == categoryIdSelected);
                            Console.WriteLine($"Are you sure you want to delete (y/n): \"{query.ToList()[cidtmp-1].CategoryName}\"");
                            if (query2.Count() > 0) {
                                Console.WriteLine("These products will also be deleted.");
                                foreach (var item in query2) {
                                    Console.WriteLine($"{item.ProductName}");
                                }
                            }   
                            string confirmdeletion = Console.ReadLine().ToLower();
                            if (confirmdeletion == "y") {
                                logger.Info($"User deleted \"{query.ToList()[cidtmp-1].CategoryName}\"");
                                foreach (var item in query2) {
                                    db.Products.Remove(item);
                                }
                                db.Categories.Remove(query.ToList()[cidtmp-1]);
                                db.SaveChanges();
                                Console.WriteLine("Item Deleted");
                            } else if (confirmdeletion == "n") {
                                Console.WriteLine("Item will not be deleted");
                            } else {
                                Console.WriteLine("Invalid input. Deletion aborted.");
                            }
                        } else {
                            Console.WriteLine("Number is out of range");
                        }
                    } else if (choice == "8") { 
                        // Display Category(-ies)
                        Console.WriteLine("1) Display Category and Desc");
                        Console.WriteLine("2) Display Specific Category and Related Products");
                        Console.WriteLine("3) Display All Categories and Related Products");

                        string dcchoice = Console.ReadLine();
                        if (dcchoice == "1") {
                            logger.Info("User selected display category's title & description");
                            // display all categories
                            var db = new Northwind_48_BTCContext();
                            var query = db.Categories.OrderBy(p => p.CategoryName);

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"{query.Count()} records returned");
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            foreach (var item in query) {
                                Console.WriteLine($"{item.CategoryName} - {item.Description}");
                            }
                            Console.ForegroundColor = ConsoleColor.White;
                        } else if (dcchoice == "2") {
                            logger.Info("User selected display category and products");
                            // display specific category with all products
                            var db = new Northwind_48_BTCContext();
                            var query = db.Categories.OrderBy(p => p.CategoryId);

                            Console.WriteLine("Select the category whose products you want to display:");
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            foreach (var item in query) {
                                Console.WriteLine($"{item.CategoryId}) {item.CategoryName}");
                            }
                            Console.ForegroundColor = ConsoleColor.White;
                            int id = int.Parse(Console.ReadLine());
                            Console.Clear();
                            logger.Info($"CategoryId {id} selected");
                            query = db.Categories.Where(c => c.CategoryId == id).Include("Products").OrderBy(p => p.CategoryId);
                            foreach (var item in query) {
                                Console.WriteLine($"{item.CategoryName}");
                                foreach (Product p in item.Products) {
                                    Console.WriteLine($"\t{p.ProductName}");
                                }
                            }
                        } else if (dcchoice == "3" ) {
                            logger.Info("User selected display all category and products");
                            // display all categories with products
                            var db = new Northwind_48_BTCContext();
                            var query = db.Categories.Include("Products").OrderBy(p => p.CategoryId);

                            foreach (var item in query){
                                Console.WriteLine($"{item.CategoryName} - {item.Description}");
                                foreach (Product p in item.Products)
                                {
                                    Console.WriteLine($"\t{p.ProductName}");
                                }
                            }
                        } else {
                            Console.WriteLine("That is not a choice");
                        }
                    }
                } while (choice.ToLower() != "q");
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }

            logger.Info("Program ended");
        }
    }
}