UPDATE t_p15345778_news_shop_project.balance_transactions
SET amount = -ABS(amount)
WHERE transaction_type = 'purchase' AND amount > 0;