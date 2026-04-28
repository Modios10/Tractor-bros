library IEEE;
use IEEE.STD_LOGIC_1164.ALL;
use IEEE.NUMERIC_STD.ALL;

entity uart_tx is
    Generic (
        CLK_FREQ : integer := 50_000_000; -- La DE10-Lite corre a 50MHz
        BAUD_RATE : integer := 115200 --baud rate de 115200
    );
	 
    Port ( --instanciaciones
        clk      : in  std_logic;
        start    : in  std_logic;
        data_in  : in  std_logic_vector(7 downto 0);
        tx_line  : out std_logic;
        busy     : out std_logic
    );
end uart_tx;

architecture rtl of uart_tx is
    -- Calculamos cuántos ciclos de reloj dura un bit
    constant BIT_PERIOD : integer := CLK_FREQ / BAUD_RATE;
    
    type state_type is (IDLE, START_BIT, DATA_BITS, STOP_BIT);
    signal state     : state_type := IDLE;
    signal clk_count : integer range 0 to BIT_PERIOD - 1 := 0;
    signal bit_index : integer range 0 to 7 := 0;
    signal tx_reg    : std_logic_vector(7 downto 0) := (others => '0');
begin
    process(clk)
    begin
        if rising_edge(clk) then
            case state is
                when IDLE =>
                    tx_line <= '1';
                    busy <= '0';
                    if start = '1' then
                        tx_reg <= data_in;
                        state <= START_BIT;
                        busy <= '1';
                    end if;

                when START_BIT =>
                    tx_line <= '0'; -- Bit de inicio siempre es '0'
                    if clk_count < BIT_PERIOD - 1 then
                        clk_count <= clk_count + 1;
                    else
                        clk_count <= 0;
                        state <= DATA_BITS;
                    end if;

                when DATA_BITS =>
                    tx_line <= tx_reg(bit_index);
                    if clk_count < BIT_PERIOD - 1 then
                        clk_count <= clk_count + 1;
                    else
                        clk_count <= 0;
                        if bit_index < 7 then
                            bit_index <= bit_index + 1;
                        else
                            bit_index <= 0;
                            state <= STOP_BIT;
                        end if;
                    end if;

                when STOP_BIT =>
                    tx_line <= '1'; -- Bit de parada siempre es '1'
                    if clk_count < BIT_PERIOD - 1 then
                        clk_count <= clk_count + 1;
                    else
                        clk_count <= 0;
                        state <= IDLE;
                    end if;
            end case;
        end if;
    end process;
end rtl;
